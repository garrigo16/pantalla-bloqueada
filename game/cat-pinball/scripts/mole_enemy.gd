extends "res://scripts/enemy.gd"
## 第二關的敵人：小鼴鼠阿吉（本體 Core）＋四個地洞（可拆部位 Hole*）。
##
##   - 洞還開著時，阿吉在開著的洞之間鑽來鑽去、從洞口丟泥巴。這時打牠只會「鏘」一聲。
##   - 球滾進開著的洞：洞被填平，球從另一個洞噴出來（地道）。
##   - 洞填到只剩 1 個：阿吉無處可躲，卡在最後那個洞裡暈頭轉向 —— 這時才打得到，也放不出招。
##   - reopen_after 秒後，阿吉把洞全部挖開，要重新填。
##
## 兩個階段：
##   - 第一階段的血打完不會結束，阿吉鑽回地底「變身」，戴著偷來的紙箱冒出來，換第二條血。
##   - 第二階段：紙箱要用球打 box_hits 下才掉（掉了 → 進寶搶回頭盔）；換洞更快、困住時間變短；
##     每隔一段時間在檯面上挖出臨時的土堆。
##
## 場景裡 armor_threshold 要設 1（剩 1 個洞就解除防禦），Core 放在最後一個子節點，才會畫在洞的上面。

## 換到第二階段（第一階段重開一局時也會送 1）
signal phase_changed(phase: int)
## 第二階段的紙箱被打掉了
signal box_lost()
## 第一條血打完、要開始變身，而且 hold_transform 開著：交給 table 播過場動畫
signal transform_started()

## 阿吉在同一個洞露頭多久才換洞
@export var hop_interval := 2.6
## 鑽進地底到從別的洞冒出來要多久
@export var dive_time := 0.35
## 被困住之後，幾秒把洞全部挖開
@export var reopen_after := 15.0
## 球在地道裡待多久
@export var tunnel_delay := 0.45
## 球從出口洞噴出來的速度
@export var tunnel_speed := 620.0

@export_group("Segundo asalto")
## 變身後的血量（第一階段的血量是 Core 的 max_hp，或 campaign.json 的 core_hp）
@export var phase2_hp := 8
## 戴著的紙箱要用球打幾下才會掉
@export var box_hits := 2
## 變身要多久（這段時間阿吉在地底，打不到也不攻擊）
@export var transform_time := 1.5
@export var hop_interval2 := 1.8
@export var reopen_after2 := 10.0
## 每隔幾秒挖一個土堆、土堆存在幾秒
@export var mound_interval := 6.0
@export var mound_life := 5.0

const DIRT := Color(0.55, 0.39, 0.26)
const BOX_COLOR := Color(0.78, 0.61, 0.42)
## 土堆可能出現的位置（檯面座標）。都避開了地洞、地道出口、障礙物、灑水器、砲台
const MOUND_SPOTS := [
	Vector2(250, 460), Vector2(420, 460), Vector2(250, 650), Vector2(420, 650),
	Vector2(335, 580), Vector2(220, 770), Vector2(450, 770),
]

var phase := 1
var box_hp := 0
## true = 變身交給過場動畫控制（table 設定）：阿吉先暈在原地，過場再依序呼叫 transform_dig()、_finish_transform()
var hold_transform := false

var _hole: Node2D
var _hop_timer := 0.0
var _dive_timer := 0.0
var _reopen_timer := 0.0
var _trapped := false
var _transforming := 0.0
var _mound_timer := 0.0
var _mounds: Array = []
# 第一階段的設定，重開一局時還原
var _hop1 := 0.0
var _reopen1 := 0.0
var _core_hp1 := -1


func _ready() -> void:
	super._ready()
	_hop1 = hop_interval
	_reopen1 = reopen_after
	_place_at(_random_open_hole())
	_hop_timer = hop_interval


func _process(delta: float) -> void:
	super._process(delta)
	if not active or not alive:
		return
	if _transforming > 0.0:
		_transforming -= delta
		if _transforming <= 0.0:
			_finish_transform()
		return
	if phase >= 2:
		_update_mounds(delta)
	if _trapped:
		_reopen_timer -= delta
		if _reopen_timer <= 0.0:
			_reopen()
		return
	if _dive_timer > 0.0:
		_dive_timer -= delta
		if _dive_timer <= 0.0:
			_surface()
		return
	_hop_timer -= delta
	# 預警中不換洞，瞄準線才不會跟著跳
	if _hop_timer <= 0.0 and not _telegraphing:
		_dive()


## 丟泥巴的是阿吉自己
func _attackers() -> Array:
	return [_core]


## 變身中不攻擊
func can_attack() -> bool:
	return _transforming <= 0.0 and super.can_attack()


func is_trapped() -> bool:
	return _trapped


func is_transforming() -> bool:
	return _transforming > 0.0


func _open_holes() -> Array:
	return _limbs.filter(func(l): return not l.destroyed)


func _random_open_hole(exclude: Node2D = null) -> Node2D:
	var open := _open_holes().filter(func(l): return l != exclude)
	if open.is_empty():
		return exclude
	return open[randi() % open.size()]


func _place_at(h: Node2D) -> void:
	if h == null:
		return
	_hole = h
	_core.position = h.position


func _dive() -> void:
	_dive_timer = dive_time
	_core.set_burrowed(true)


func _surface() -> void:
	_place_at(_random_open_hole(_hole))
	_core.set_burrowed(false)
	_hop_timer = hop_interval


## 洞填到剩最後一個：阿吉被困住
func _refresh_armor() -> void:
	super._refresh_armor()
	if _core.armored or _trapped or not alive or _transforming > 0.0:
		return
	_trapped = true
	_reopen_timer = reopen_after
	_dive_timer = 0.0
	var open := _open_holes()
	_place_at(open[0] if not open.is_empty() else _hole)
	_core.set_burrowed(false)
	_core.dizzy = true
	Juice.shake(10.0)


## 阿吉把洞全部挖開，重新躲起來
func _reopen() -> void:
	_trapped = false
	_core.dizzy = false
	_revive_holes()
	_refresh_armor()
	_hop_timer = hop_interval
	Juice.play("break", 0.7, -6.0)


func _revive_holes() -> void:
	for l in _limbs:
		if l.destroyed:
			l.revive()
			Juice.burst(l.global_position, DIRT, 10, 220.0)


# ── 兩個階段 ────────────────────────────────────────────────

## 第一階段的血打完：不結束，改成變身
func _on_core_killed() -> void:
	if phase == 1:
		_start_transform()
	else:
		_clear_mounds()
		super._on_core_killed()


func _start_transform() -> void:
	_transforming = transform_time
	_trapped = false
	_dive_timer = 0.0
	if hold_transform:
		# 過場動畫接手：阿吉暈在原地、血圈歸零（不算打爆，才畫得出暈頭轉向的樣子）
		_core.revive()
		_core.hp = 0
		_core.dizzy = true
		_core.set_burrowed(false)
		transform_started.emit()
		return
	transform_dig()
	Juice.number(_core.global_position + Vector2(-48.0, -64.0), "¡Aji se transforma!", Color(1.0, 0.78, 0.35), 30)


## 變身的第一步：鑽回地底，洞全部挖開
func transform_dig() -> void:
	_core.dizzy = false
	_core.set_burrowed(true)
	_revive_holes()
	_refresh_armor()
	Juice.shake(20.0)
	Juice.play("break", 0.6, -2.0)


func _finish_transform() -> void:
	_transforming = 0.0
	phase = 2
	_core.max_hp = phase2_hp
	_core.revive()
	box_hp = box_hits
	_core.box = box_hp
	hop_interval = hop_interval2
	reopen_after = reopen_after2
	_mound_timer = mound_interval * 0.5
	_place_at(_random_open_hole())
	_core.set_burrowed(false)
	_hop_timer = hop_interval
	Juice.burst(_core.global_position, DIRT, 18, 300.0)
	Juice.play("launch", 0.7, -4.0)
	phase_changed.emit(2)


## 變身過場的預設台詞（campaign.json 的 level.phase2_cutscene 可以覆蓋）
const CUTSCENE_LINES := {
	"hurt": "Ay, ay… ¡Bah! ¿Creíais que ya habíais ganado?",
	"dig": "¡Brrrum!",
	"box": "¡Contemplad mi cabezazo de cartón! ¡Ahora voy en serio!",
	"jinbao": "¡Ese es mi casco de caballero! ¡Devuélvemelo!",
	"title": "Segundo asalto",
	"hint": "Golpea el casco de Aji con el ovillo para devolvérselo a Jinbao.",
}


## 變身過場的每一段，交給 PhaseCutscene 播
func cutscene_beats(lines: Dictionary) -> Array:
	var l: Dictionary = CUTSCENE_LINES.duplicate()
	l.merge(lines, true)
	return [
		{"time": 2.2, "who": "Aji, el topo", "at": "core", "text": l["hurt"]},
		{"time": 1.2, "shout": l["dig"], "enter": transform_dig},
		{"time": 2.2, "who": "Aji, el topo", "at": "core", "text": l["box"], "enter": _cutscene_pop},
		{"time": 1.8, "who": "Jinbao", "at": "TurretRight", "text": l["jinbao"]},
		{"time": 2.0, "title": l["title"], "sub": l["hint"],
			"enter": func() -> void: Juice.play("launch", 0.8, -4.0)},
	]


func _cutscene_pop() -> void:
	_finish_transform()
	Juice.shake(12.0)


## 播不了過場時（例如同一瞬間失手）直接變身完
func finish_transform_now() -> void:
	transform_dig()
	_finish_transform()


## 紙箱被球打到一下。打掉 → 進寶搶回頭盔（由 table 接 box_lost 處理）
func _hit_box(pos: Vector2) -> void:
	box_hp -= 1
	_core.box = box_hp
	_core.flash_hit()
	Combat.report_hit(pos, 1, "part", box_hp == 0)
	if box_hp == 0:
		Juice.number(_core.global_position + Vector2(-56.0, -64.0), "¡Adiós, casco de cartón!", Color(1.0, 0.86, 0.4), 26)
		Juice.burst(_core.global_position, BOX_COLOR, 16, 320.0)
		box_lost.emit()


# ── 土堆（第二階段）──────────────────────────────────────────

func _update_mounds(delta: float) -> void:
	_mounds = _mounds.filter(func(m): return is_instance_valid(m))
	if _trapped:
		return
	_mound_timer -= delta
	if _mound_timer > 0.0:
		return
	_mound_timer = mound_interval
	var table := get_parent()
	var ball = table.get("ball")
	var spots: Array = []
	for p in MOUND_SPOTS:
		if ball != null and (ball as Node2D).global_position.distance_to(p) < 80.0:
			continue
		if _mounds.any(func(m): return m.global_position.distance_to(p) < 60.0):
			continue
		spots.append(p)
	if spots.is_empty():
		return
	var mound := Mound.new()
	mound.life = mound_life
	mound.position = spots[randi() % spots.size()]
	table.add_child(mound)
	_mounds.append(mound)


func _clear_mounds() -> void:
	for m in _mounds:
		if is_instance_valid(m):
			m.queue_free()
	_mounds.clear()


# ── 被打到 ──────────────────────────────────────────────────

func _on_part_hit(part, ball) -> void:
	if not alive:
		return
	if _transforming > 0.0:
		# 變身中打不到阿吉，但地道照常運作
		if part.hole:
			_tunnel(part, ball)
		return
	if part == _core and box_hp > 0:
		_hit_box(ball.global_position)
		return
	if part.hole:
		# 困住時最後一個洞被阿吉擋著，不再計算
		if _trapped:
			return
		_apply_damage(part, limb_damage, ball.global_position)
		_tunnel(part, ball)
		return
	super._on_part_hit(part, ball)


## 地道：球從另一個開著的洞噴出來
func _tunnel(entry: Node2D, ball) -> void:
	if not ball.has_method("dive") or ball.is_diving():
		return
	# 出口：別的開著的洞，而且不是阿吉正在探頭的那個（會卡到牠）
	var exits := _open_holes().filter(
		func(l): return l != entry and not (l == _hole and _core.visible))
	var exit: Node2D = exits[randi() % exits.size()] if not exits.is_empty() else entry
	# 出口在洞口正上方、偵測範圍外面，球冒出來才不會馬上又掉回同一個洞
	var out := exit.global_position + Vector2(0.0, -(exit.radius + 22.0))
	var vel := Vector2(randf_range(-0.4, 0.4), -1.0).normalized() * tunnel_speed
	ball.dive(out, tunnel_delay, vel)
	Juice.burst(entry.global_position, DIRT, 10, 200.0)
	Juice.play("sling", 0.55, -6.0)
	get_tree().create_timer(tunnel_delay).timeout.connect(
		func() -> void:
			Juice.burst(out, DIRT, 12, 260.0)
	)


## 砲台的小魚乾：洞還開著就去塞最近的洞（阿吉探頭的那個除外），困住時打阿吉。
## 紙箱只能用球打，所以阿吉還戴著紙箱時小魚乾不打牠
func nearest_target(from: Vector2) -> Node2D:
	if not alive or _transforming > 0.0:
		return null
	if _trapped:
		return null if (_core.destroyed or box_hp > 0) else _core
	var best: Node2D = null
	var best_d := INF
	for l in _open_holes():
		if l == _hole and _core.visible:
			continue
		var d: float = from.distance_to(l.global_position)
		if d < best_d:
			best_d = d
			best = l
	return best


## 砲台找不到目標時要跟玩家說什麼。
## 阿吉被困住、頭上還戴著紙箱時小魚乾是打不動的，那時候只能用球砸
func no_target_hint() -> String:
	if _trapped and box_hp > 0:
		return "¡Golpea el cartón con el ovillo!"
	if _trapped:
		return "¡Aji está atrapado! ¡Dale!"
	return "¡Túneles taponados!"


func take_bullet(part, dmg: int, pos: Vector2) -> void:
	if _transforming > 0.0:
		return
	# 困住時不能把最後一個洞也塞掉；紙箱只能用球打
	if _trapped and part != _core:
		return
	if part == _core and box_hp > 0:
		return
	super.take_bullet(part, dmg, pos)


func reset() -> void:
	# 第一次重開時記下第一階段的血量（table 已經套用過 campaign.json 的 core_hp）
	if _core_hp1 < 0:
		_core_hp1 = _core.max_hp
	_core.max_hp = _core_hp1
	phase = 1
	box_hp = 0
	_core.box = 0
	hop_interval = _hop1
	reopen_after = _reopen1
	_transforming = 0.0
	_mound_timer = 0.0
	_clear_mounds()
	super.reset()
	_trapped = false
	_core.dizzy = false
	_core.set_burrowed(false)
	_dive_timer = 0.0
	_place_at(_random_open_hole())
	_hop_timer = hop_interval
	phase_changed.emit(1)
