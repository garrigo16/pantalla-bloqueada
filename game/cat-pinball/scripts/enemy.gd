extends Node2D
## 敵人的共用行為：本體（Core）＋可拆部位（其他子節點）＋預警後干擾檯面。
##
## 第一關：大嘴浣熊（本體）＋兩隻松鼠小弟（可拆部位），就是這個腳本本身。
## 第二關：小鼴鼠阿吉＋四個地洞，見 mole_enemy.gd（繼承這個腳本）。
##
## 設計文件 3.1 / 3.3：
##   - 牠不是螢幕邊上的一條血條，是檯面上的實體。打掉一塊就少一塊。
##   - 牠不「打你」，牠讓檯面對你更不友善 —— 朝可樂娜的爪或彈射器丟東西，
##     爪子痛得縮回去、彈射器卡住，那側檯面瞬間變難守。
##   - 可拆部位剩下的數量 <= armor_threshold 時，本體的防禦解除，也放不出招。

signal interference(target: String, duration: float)
## 預警開始，並且已經決定要打哪裡（讓 table 畫瞄準線）
signal telegraph_started(target: String)
## 預警中途被打斷（剛好在這時失去攻擊能力）
signal telegraph_cancelled()
signal defeated()

## 幾秒放一次招
@export var attack_interval := 9.0
## 一次讓彈射器卡住多久
@export var attack_duration := 6.0
## 一次讓翼板縮短多久
@export var shrink_duration := 3.0
## 攻擊目標輪替表。丟過去的東西砸到什麼就發生什麼：
## 翼板 -> 縮短；彈射器 -> 停止踢球。
@export var attack_targets: Array[String] = [
	"FlipperLeft", "SlingshotRight", "FlipperRight", "SlingshotLeft",
]
## 放招前的預警時間
@export var telegraph := 1.1
## 丟出去的東西長什麼樣子：acorn（橡實）、mud（泥巴）
@export var projectile := "acorn"
## 可拆部位剩下幾個以下，本體的防禦就解除、也放不出招。
## 第一關 0 = 兩隻松鼠都打跑；第二關 1 = 地洞填到剩最後一個
@export var armor_threshold := 0

@export var limb_damage := 1
@export var core_damage := 1

var active := false
var alive := true

var _limbs: Array = []
var _core
var _timer := 0.0
var _telegraphing := false
var _next_target := 0


func _ready() -> void:
	_core = $Core
	for c in get_children():
		if c == _core:
			continue
		_limbs.append(c)
	for p in _limbs + [_core]:
		p.part_hit.connect(_on_part_hit)
	_refresh_armor()
	_timer = attack_interval


func _process(delta: float) -> void:
	queue_redraw()
	# 預警時丟東西的角色把東西舉起來。牠們是子節點、畫在本節點之後，所以交給牠們自己畫
	var wind := 0.0
	if _telegraphing and alive:
		wind = 0.5 + 0.5 * sin(Time.get_ticks_msec() / 70.0)
	for a in _attackers():
		if a.wind_up != wind:
			a.wind_up = wind
			a.queue_redraw()
	if not active or not alive:
		return
	if not can_attack():
		# 放不出招了。但如果剛好在預警途中失去攻擊能力，
		# 一定要把預警收掉 —— 否則 _fire() 永遠不會執行，
		# 瞄準線和紅圈會永久掛在畫面上。
		if _telegraphing:
			_telegraphing = false
			_timer = attack_interval
			telegraph_cancelled.emit()
		return

	_timer -= delta
	if _telegraphing:
		if _timer <= 0.0:
			_telegraphing = false
			_timer = attack_interval
			_fire()
	elif _timer <= telegraph:
		_telegraphing = true
		telegraph_started.emit(attack_targets[_next_target])
		Juice.play("attack", 1.0, -4.0)


func living_limbs() -> int:
	var n := 0
	for l in _limbs:
		if not l.destroyed:
			n += 1
	return n


## 還放得出招嗎
func can_attack() -> bool:
	return living_limbs() > armor_threshold


## 負責丟東西的角色。第一關是還在的松鼠
func _attackers() -> Array:
	return _limbs


func _refresh_armor() -> void:
	_core.armored = living_limbs() > armor_threshold


func _fire() -> void:
	var t: String = attack_targets[_next_target]
	_next_target = (_next_target + 1) % attack_targets.size()
	interference.emit(t, shrink_duration if t.begins_with("Flipper") else attack_duration)


## 過場動畫開始時：收掉預警、重新計時
func cancel_attack() -> void:
	if _telegraphing:
		_telegraphing = false
		telegraph_cancelled.emit()
	_timer = attack_interval


func core_position() -> Vector2:
	return _core.global_position


## 攻擊從丟東西的角色手上出發（世界座標）。都不在時退回本體的位置。
func throw_origins() -> Array:
	var out: Array = []
	for a in _attackers():
		if not a.destroyed:
			out.append(a.global_position + Vector2(0.0, -a.radius * 0.6))
	if out.is_empty():
		out.append(_core.global_position)
	return out


func _on_part_hit(part, ball) -> void:
	if not alive:
		return
	var pos: Vector2 = ball.global_position

	if part == _core and _core.armored:
		# 打不動。給一聲金屬回饋，讓玩家知道「不是沒打到，是打不進去」
		Combat.report_hit(pos, 0, "armor")
		return

	_apply_damage(part, core_damage if part == _core else limb_damage, pos)


## 砲台子彈用：找最近的可打目標。部位還在就打部位，都沒了才打本體。
## 回傳 null 代表沒東西可打（本體還在防禦中，或敵人已死）。
func nearest_target(from: Vector2) -> Node2D:
	var best: Node2D = null
	var best_d := INF
	for l in _limbs:
		if l.destroyed:
			continue
		var d: float = from.distance_to(l.global_position)
		if d < best_d:
			best_d = d
			best = l
	if best != null:
		return best
	if not _core.destroyed and not _core.armored:
		return _core
	return null


func take_bullet(part, dmg: int, pos: Vector2) -> void:
	if not alive or part == null or part.destroyed:
		return
	if part == _core and _core.armored:
		return
	_apply_damage(part, dmg, pos)


func _apply_damage(part, dmg: int, pos: Vector2) -> void:
	var killed: bool = part.take_damage(dmg)
	Combat.report_hit(pos, dmg, "core" if part == _core else "part", killed)

	if killed and part != _core:
		_refresh_armor()
	elif killed and part == _core:
		_on_core_killed()


## 本體被打倒。第二關的阿吉會改成變身進入第二階段
func _on_core_killed() -> void:
	alive = false
	active = false
	defeated.emit()


func reset() -> void:
	for p in _limbs + [_core]:
		p.revive()
	alive = true
	_telegraphing = false
	_timer = attack_interval
	_next_target = 0
	_refresh_armor()


func _draw() -> void:
	if _telegraphing and alive:
		# 預警：丟東西的角色身上出現紅圈
		var pulse: float = 0.5 + 0.5 * sin(Time.get_ticks_msec() / 70.0)
		for a in _attackers():
			if not a.destroyed and a.visible:
				CatArt.throw_charge(self, a.position, a.radius, pulse)
