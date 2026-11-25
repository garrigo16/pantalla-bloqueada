extends "res://scripts/enemy.gd"
## 第三關的敵人：巴哥老大（本體 Core）＋紙箱城牆（可拆部位 Box*，真的會擋球）。
##
## 第一階段（滑梯城堡）：
##   - 巴哥趴在滑梯頂端，前面一排紙箱。紙箱剩 armor_threshold 個以下，巴哥才打得到
##   - 重踏步：抬腳預警，踩下去畫面一震，場上的球被震飛、亂飄一陣子（兩個階段都一樣，紙箱不會回來）
##   - 丟玩具球（projectile = toyball）
##   - 小刺蝟從左右的小推車（CartLeft／CartRight）輪流滾向雪兒、進寶的砲台，
##     刺到的砲台一段時間內要打 2 下才開砲（見 hedgehog.gd）
## 第一條血打完：城堡倒塌（table 播過場），過場完進寶飛天撲擊 = 雙球
## 第二階段（大暴走）：紙箱沒了，巴哥跳到城牆的位置左右跑、一直打得到；改丟骨頭；踩得更頻繁
##
## 場景裡 armor_threshold 設 2，Core 放在最後一個子節點（畫在紙箱上面）。

## 換階段（重開一局時送 1）
signal phase_changed(phase: int)
## 第一條血打完、要開始變身，而且 hold_transform 開著：交給 table 播過場動畫
signal transform_started()
## 踩下去了
signal stomped(phase: int)

@export_group("Pisotón")
## 開局後幾秒踩第一次（讓玩家早點看到這招）
@export var first_stomp := 6.0
@export var stomp_interval := 12.0
@export var stomp_interval2 := 8.0
## 抬腳預警多久
@export var stomp_warn := 1.2
## 踩下去那一下，把場上的球震飛的力道
@export var stomp_nudge := 320.0
## 踩完之後檯面晃幾秒（球亂飄），以及每下推多大力
@export var stomp_wobble := 1.2
@export var stomp_wobble_force := 180.0

@export_group("Segundo asalto")
@export var phase2_hp := 14
## 跳下滑梯後站的高度（本節點座標，就是紙箱城牆的位置）
@export var run_y := -10.0
## 左右跑的範圍（離中間多遠）
@export var run_range := 140.0
@export var run_speed := 120.0

@export_group("Erizos")
## 幾秒從小推車滾出一隻：第一階段、第二階段
@export var hedgehog_interval := 8.0
@export var hedgehog_interval2 := 5.0
## 場上最多幾隻
@export var max_hedgehogs := 2
@export var hedgehog_speed := 200.0
@export var hedgehog_speed2 := 260.0
## 滾到砲台上時，砲台被刺到幾秒（這段時間要打 2 下才開砲）
@export var hedgehog_prick := 12.0

## 變身過場的預設台詞（campaign.json 的 level.phase2_cutscene 可以覆蓋）
const CUTSCENE_LINES := {
	"hurt": "¡Auu! ¡Mi castillo! ¡Malditas bolitas de pelo!",
	"collapse": "¡Cataplum! ¡Crash!",
	"angry": "¡El rey está furioso! ¡Os voy a aplastar!",
	"jinbao": "¡Allá va el salto volador del Caballero Oscuro!",
	"title": "Asalto final",
	"hint": "¡Carlino no para de moverse! ¡A por él con los dos ovillos!",
}
const BOX_COLOR := Color(0.78, 0.61, 0.42)
const DUST := Color(0.86, 0.80, 0.68)

var phase := 1
## true = 變身交給過場動畫控制（table 設定）：巴哥先暈在滑梯頂，過場再依序倒塌、跳下來
var hold_transform := false

var _transforming := false
var _stomp_timer := 0.0
## 踏步預警剩幾秒（> 0 = 正在抬腳）
var _stomp_left := 0.0
var _run_dir := 1.0
var _core_home := Vector2.ZERO
var _core_hp1 := -1
var _projectile1 := ""
## 第二階段開始了但還在播過場：過場播完才放第二顆球
var _multiball_pending := false
## 踩完之後檯面還要晃幾秒、距離下一次推球還有幾秒
var _wobble_left := 0.0
var _wobble_tick := 0.0
var _hedgehog_timer := 0.0
## 下一隻小刺蝟從哪台推車出發（左右輪流）
var _next_cart := 0


func _ready() -> void:
	super._ready()
	_core_home = _core.position
	_projectile1 = projectile
	_stomp_timer = first_stomp
	_hedgehog_timer = hedgehog_interval * 0.5


func _process(delta: float) -> void:
	super._process(delta)
	if not active or not alive or _transforming:
		return
	if phase >= 2:
		_run(delta)
	_update_stomp(delta)
	_update_wobble(delta)
	_update_hedgehogs(delta)


## 丟東西的是巴哥自己
func _attackers() -> Array:
	return [_core]


## 抬腳預警、變身中不丟東西。紙箱被打光也照丟（最終關）
func can_attack() -> bool:
	return alive and not _transforming and _stomp_left <= 0.0


func is_stomping() -> bool:
	return _stomp_left > 0.0


func is_transforming() -> bool:
	return _transforming


# ── 重踏步 ──────────────────────────────────────────────────

func _update_stomp(delta: float) -> void:
	if _stomp_left > 0.0:
		_stomp_left -= delta
		if _stomp_left <= 0.0:
			_stomp()
		return
	_stomp_timer -= delta
	# 丟東西的預警中不踩，兩個預警才不會疊在一起
	if _stomp_timer <= 0.0 and not _telegraphing:
		_stomp_left = stomp_warn
		_core.stomping = true
		Juice.play("attack", 0.6, -4.0)
		Juice.number(_core.global_position + Vector2(-78.0, -_core.radius - 46.0), "¡Carlino va a dar un pisotón!",
			Color(1.0, 0.42, 0.42), 26)


func _stomp() -> void:
	_stomp_left = 0.0
	_core.stomping = false
	_stomp_timer = stomp_interval2 if phase >= 2 else stomp_interval
	Juice.shake(32.0)
	Juice.play("break", 0.5, -1.0)
	Juice.burst(_core.global_position + Vector2(0.0, _core.radius), DUST, 20, 320.0)
	Juice.number(_core.global_position + Vector2(-22.0, -10.0), "¡Pum!", Color(1.0, 0.86, 0.4), 44)
	# 震波沿著檯面揚起一排灰塵
	for l in _limbs:
		Juice.burst(l.global_position + Vector2(0.0, 18.0), DUST, 8, 220.0)
	# 整張檯面一震：場上的球被震飛，接著亂飄一陣子（紙箱不會回來）
	_nudge_balls(stomp_nudge)
	_wobble_left = stomp_wobble
	_wobble_tick = 0.15
	stomped.emit(phase)


## 踩完之後檯面還在晃：每 0.15 秒把球往隨機方向推一下
func _update_wobble(delta: float) -> void:
	if _wobble_left <= 0.0:
		return
	_wobble_left -= delta
	_wobble_tick -= delta
	if _wobble_tick > 0.0:
		return
	_wobble_tick = 0.15
	_nudge_balls(stomp_wobble_force)


## 把場上的球往隨機方向推一下：主要往左右，稍微往上。
## 定住的、在地道裡的、還在發射道裡的球不推
func _nudge_balls(force: float) -> void:
	var table := get_parent()
	if not table.has_method("all_balls"):
		return
	for b in table.all_balls():
		if b.freeze or b.is_diving() or b.global_position.x > 626.0:
			continue
		var dir := Vector2(randf_range(0.6, 1.0) * (1.0 if randf() < 0.5 else -1.0), randf_range(-0.7, 0.1))
		b.apply_central_impulse(dir.normalized() * force)


# ── 小刺蝟 ──────────────────────────────────────────────────

func _update_hedgehogs(delta: float) -> void:
	_hedgehog_timer -= delta
	if _hedgehog_timer > 0.0:
		return
	_hedgehog_timer = hedgehog_interval2 if phase >= 2 else hedgehog_interval
	if get_tree().get_nodes_in_group("hedgehogs").size() >= max_hedgehogs:
		return
	var table := get_parent()
	var carts: Array = [table.get_node_or_null("CartLeft"), table.get_node_or_null("CartRight")].filter(
		func(c): return c != null)
	if carts.is_empty():
		return
	var cart = carts[_next_cart % carts.size()]
	_next_cart += 1
	var h := Hedgehog.new()
	h.home = cart.position
	h.position = cart.position + Vector2(0.0, 24.0)
	h.aim = cart.aim
	h.target = cart.target
	h.speed = hedgehog_speed2 if phase >= 2 else hedgehog_speed
	h.prick = hedgehog_prick
	table.add_child(h)
	Juice.play("sling", 0.5, -6.0)
	Juice.number(cart.position + Vector2(-52.0, -40.0), "¡Llegan los erizos!",
		Color(1.0, 0.86, 0.4), 20)


func _clear_hedgehogs() -> void:
	for h in get_tree().get_nodes_in_group("hedgehogs"):
		Juice.burst(h.global_position, DUST, 8, 200.0)
		h.remove_from_group("hedgehogs")
		h.queue_free()


# ── 第二階段：左右跑 ────────────────────────────────────────

func _run(delta: float) -> void:
	# 抬腳預警時站定
	if _stomp_left > 0.0:
		return
	var p: Vector2 = _core.position
	p.x += _run_dir * run_speed * delta
	if absf(p.x - _core_home.x) > run_range:
		p.x = _core_home.x + signf(p.x - _core_home.x) * run_range
		_run_dir = -_run_dir
	_core.position = p


# ── 兩個階段 ────────────────────────────────────────────────

func _on_core_killed() -> void:
	# 過場動畫、打贏的時候，場上的小刺蝟都跑掉
	_clear_hedgehogs()
	if phase == 1:
		_start_transform()
	else:
		super._on_core_killed()


func _start_transform() -> void:
	_transforming = true
	_stomp_left = 0.0
	_core.stomping = false
	if hold_transform:
		# 過場動畫接手：巴哥暈在滑梯頂、血圈歸零（不算打爆，才畫得出暈頭轉向的樣子）
		_core.revive()
		_core.hp = 0
		_core.dizzy = true
		transform_started.emit()
		return
	finish_transform_now()


## 城堡倒塌：紙箱全部垮掉，第二階段不會再被震回來
func collapse_castle() -> void:
	_core.dizzy = false
	for l in _limbs:
		Juice.burst(l.global_position, BOX_COLOR, 16, 340.0)
		l.destroy()
		# 倒掉的紙箱整個不見，不留一團煙
		l.visible = false
	_refresh_armor()
	Juice.shake(26.0)
	Juice.play("break", 0.55, 0.0)


## 巴哥氣得跳下滑梯，換第二條血
func _finish_transform() -> void:
	_transforming = false
	phase = 2
	_core.dizzy = false
	_core.angry = true
	_core.max_hp = phase2_hp
	_core.revive()
	_core.position = Vector2(_core_home.x, run_y)
	projectile = "bone"
	_stomp_timer = stomp_interval2
	_timer = attack_interval
	Juice.burst(_core.global_position, DUST, 22, 360.0)
	phase_changed.emit(2)
	if hold_transform:
		_multiball_pending = true
	else:
		_multiball()


## 播不了過場時（例如同一瞬間失手）直接變身完
func finish_transform_now() -> void:
	collapse_castle()
	_finish_transform()
	if _multiball_pending:
		_multiball_pending = false
		_multiball()


## 過場播完（table 呼叫）：進寶飛天撲擊
func after_cutscene(_table) -> void:
	if _multiball_pending:
		_multiball_pending = false
		_multiball()


func _multiball() -> void:
	var table := get_parent()
	if table.has_method("spawn_extra_ball"):
		table.spawn_extra_ball("jinbao")


## 城堡倒塌過場的每一段，交給 PhaseCutscene 播
func cutscene_beats(lines: Dictionary) -> Array:
	var l: Dictionary = CUTSCENE_LINES.duplicate()
	l.merge(lines, true)
	return [
		{"time": 2.4, "who": "Jefe Carlino", "at": "core", "text": l["hurt"]},
		{"time": 1.4, "shout": l["collapse"], "enter": collapse_castle},
		{"time": 2.2, "who": "Jefe Carlino", "at": "core", "text": l["angry"], "enter": _cutscene_jump},
		{"time": 2.0, "who": "Jinbao", "at": "TurretRight", "text": l["jinbao"]},
		{"time": 2.0, "title": l["title"], "sub": l["hint"],
			"enter": func() -> void: Juice.play("launch", 0.8, -4.0)},
	]


func _cutscene_jump() -> void:
	_finish_transform()
	Juice.shake(16.0)


# ── 被打到 ──────────────────────────────────────────────────

func _on_part_hit(part, ball) -> void:
	if _transforming:
		return
	super._on_part_hit(part, ball)


func take_bullet(part, dmg: int, pos: Vector2) -> void:
	if _transforming:
		return
	super.take_bullet(part, dmg, pos)


## 砲台的小魚乾：巴哥打得到就打巴哥，不然打最近的紙箱
func nearest_target(from: Vector2) -> Node2D:
	if not alive or _transforming:
		return null
	if not _core.destroyed and not _core.armored:
		return _core
	return super.nearest_target(from)


func reset() -> void:
	# 第一次重開時記下第一階段的血量（table 已經套用過 campaign.json 的 core_hp）
	if _core_hp1 < 0:
		_core_hp1 = _core.max_hp
	_core.max_hp = _core_hp1
	phase = 1
	_transforming = false
	_multiball_pending = false
	projectile = _projectile1
	_core.position = _core_home
	_core.angry = false
	_core.dizzy = false
	_core.stomping = false
	_stomp_left = 0.0
	_stomp_timer = first_stomp
	_wobble_left = 0.0
	_run_dir = 1.0
	_hedgehog_timer = hedgehog_interval * 0.5
	_clear_hedgehogs()
	for l in _limbs:
		l.visible = true
	super.reset()
	phase_changed.emit(1)
