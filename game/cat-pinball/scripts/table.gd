class_name Table
extends Node2D
## 檯面。牆壁生成、發球、漏球、球數、以及把各元件接上 Combat。
##
## 牆壁不用 CollisionPolygon2D，而是直接餵 segments 給 ConcavePolygonShape2D。
## 原因：CollisionPolygon2D 的 Segments 模式會自動把折線頭尾接起來，
## 漏球口與外道出口就全被封死了。用 segments 可以精準控制哪裡有牆、哪裡是洞。
##
## 想改檯面配置，改下面幾個常數的座標就好，存檔重跑即可。

## 主外框：左外道外牆 → 左牆 → 上方拱形 → 發射道外牆 → 發射道底 → 分隔牆 → 導球彎
const WALL_MAIN: Array[Vector2] = [
	Vector2(175, 1252),   # 左外道出口（球從這裡掉進 drain，繞過翼板）
	Vector2(140, 1215),
	Vector2(55, 1005),
	Vector2(40, 940),
	Vector2(40, 400),     # 左牆
	Vector2(64.7, 275.6), # ── 上方拱形：圓心 (365,400)，半徑 325 ──
	Vector2(135.2, 170.2),
	Vector2(240.6, 99.7),
	Vector2(365, 75),
	Vector2(489.4, 99.7),
	Vector2(594.8, 170.2),
	Vector2(665.3, 275.6),
	Vector2(690, 400),
	Vector2(690, 1216),   # 發射道外側牆
	Vector2(630, 1216),   # 發射道底
	Vector2(630, 340),    # 發射道／檯面 分隔牆
	Vector2(622, 300),    # 頂端往左彎，把球導進檯面
	Vector2(600, 268),
	Vector2(568, 250),
]

## 右外道外牆（從發射道分隔牆上分岔）
const WALL_RIGHT_LOWER: Array[Vector2] = [
	Vector2(630, 940),
	Vector2(615, 1005),
	Vector2(530, 1215),
	Vector2(495, 1252),
]

## 外道／內道 分隔島。左邊是外道（死），右邊是內道（球被送回翼板）。
## 末端刻意伸到翼板支點的**右側**（超過 x=218 / x=452）。
## 若末端停在支點左下方，球會卡死在「分隔島末端 ↔ 翼板底部」的 V 形夾角裡：
## 兩個接觸法線一個朝右上、一個朝左上，剛好把重力夾住，是穩定平衡，球出不來。
## 伸過去之後，球滾到底是直接落在翼板上表面，會繼續往尖端滾。
const WALL_LEFT_DIVIDER: Array[Vector2] = [
	Vector2(98, 1005),
	Vector2(128, 1068),
	Vector2(162, 1098),
	Vector2(228, 1130),
]

const WALL_RIGHT_DIVIDER: Array[Vector2] = [
	Vector2(572, 1005),
	Vector2(542, 1068),
	Vector2(508, 1098),
	Vector2(442, 1130),
]

## 作弊用安全網。V 字形，把球漏斗到中央漏球口正下方再彈回檯面。
## 兩端刻意落在左右外道外牆上（(140,1215)-(175,1252) 與其鏡像），把出口完全封死。
const NET_LINE: Array[Vector2] = [
	Vector2(152, 1228),
	Vector2(335, 1252),
	Vector2(518, 1228),
]

const WALL_WIDTH := 7.0

## 發射道的彈簧：擋板頂端的靜止高度、壓到底的行程、彈簧底座、擋板寬度、放開後彈回原位的時間
const PLUNGER_REST := 1158.0
const PLUNGER_TRAVEL := 34.0
const PLUNGER_FLOOR := 1208.0
const PLUNGER_WIDTH := 40.0
const PLUNGER_SNAP := 0.08
const BALL_SCENE := preload("res://scenes/ball.tscn")

## 外道救球彈簧：左右外道各一個感應點（外道通道正中間，碰不到內道），與把球踢回去的方向
const KICKBACK_SPOTS: Array[Vector2] = [Vector2(105, 1068), Vector2(565, 1068)]
const KICKBACK_DIRS: Array[Vector2] = [Vector2(-0.1, -1.0), Vector2(0.1, -1.0)]

enum State { PLAYING, OVER, WIN, CUTSCENE }

@export var balls_per_game := 4
## 背景：porch（第一關的木頭走廊）、park（第二關的公園草地）
@export var backdrop := "porch"
## 球在發射道裡的起始位置（放下去會落在彈簧擋板上）
@export var lane_spawn := Vector2(660, 1140)
@export var launch_min := 620.0
@export var launch_max := 1150.0
@export var charge_time := 0.8

@export_group("Red de seguridad")
## 開啟後三個漏球口都被彈簧封住，球不會掉。用來跑流程。
@export var cheat_net := false
## 彈簧把球往上彈的力道
@export var spring_force := 700.0
## 球低於這條線就啟動彈簧
@export var spring_trigger_y := 1196.0

@export_group("Rescate de Nieve")
## 草叢小徑三盞全亮後，漏球口被彈簧封住、沒有時間限制；
## 救起一次球、而且球彈回這個高度以上才收網（只有放了 Lanes 節點的關卡才有）
@export var rescue_release_y := 950.0

@export_group("Transición")
## 第二關阿吉第一條血打完時播過場動畫（關掉就只跳「阿吉變身！」字樣、直接變身）
@export var phase_cutscene := true

@export_group("Rescates laterales")
## 球掉進左右外道時，彈簧把球踢回檯面（三關共用）。踢過一次要等幾秒才能再用
@export var kickback_cooldown := 15.0
@export var kickback_speed := 1050.0

@export_group("Final de la partida")
## 「失手了」停幾秒才進失敗劇情（真人試玩：2.2 秒太慢）
@export var lose_delay := 1.0
## 「解決了」停幾秒才進結尾劇情。要讓勝利那首曲子放完（5.5 秒）、彩帶也撒得完
@export var win_delay := 5.5

@onready var ball: RigidBody2D = $Ball
## 雙球（第三關）：主球 ball 以外、同時在場上的球。掉下去不扣命
var extra_balls: Array = []
@onready var enemy = $Enemy
## 草叢小徑（第二關）。沒有這個節點的關卡就沒有雪兒救援
@onready var lanes = get_node_or_null("Lanes")
## 旋轉灑水器（第二關）。敵人換階段時跟著變速
@onready var sprinkler = get_node_or_null("Sprinkler")

var balls_left := 0
var state := State.PLAYING

var _charge := 0.0
var _charging := false
var chapter_title := ""
var _aim_node: Node2D
var _net_shape: CollisionShape2D
var _spring_next := 0.0
var _net_flash := 0.0
## 雪兒救援待命中：漏球口被封住，救起一次球就收網
var _rescue_armed := false
## 這次救援已經把球彈起來過，等球彈到 rescue_release_y 以上就收網
var _rescue_used := false
var _plunger: AnimatableBody2D
var _plunger_shape: CollisionShape2D
## 放開彈簧後經過的時間（負數 = 沒在回彈）
var _snap_t := -1.0
var _snap_from := PLUNGER_REST
var _cutscene: PhaseCutscene
## 關卡開場的任務卡（球一發出去就收掉）
var _brief: LevelBrief
var _pause_button: PauseButton
## 過場用空白鍵跳過後，要先放開空白鍵才能蓄力（不然一放開就把球打出去）
var _wait_release := false
## 砲台「沒東西可打」的提示下次最早什麼時候能再跳
var _no_target_at := 0.0
## 外道救球彈簧（左、右）：還要幾秒才充好（0 = 可以用）、剛踢過的閃光
var _kick_wait: Array[float] = [0.0, 0.0]
var _kick_flash: Array[float] = [0.0, 0.0]


func _ready() -> void:
	Juice.camera = $Camera2D
	_setup_hud()
	_pause_button = PauseButton.new()
	_pause_button.toggled.connect(toggle_pause)
	add_child(_pause_button)
	_play_level_music()
	var bg := Backdrop.new()
	bg.style = backdrop
	add_child(bg)
	_build_walls()
	_build_plunger()
	_build_kickbacks()

	$Drain.body_entered.connect(_on_drain_body_entered)
	for b in get_tree().get_nodes_in_group("bumpers"):
		b.bumper_hit.connect(_on_bumper_hit)
		b.bumper_charged.connect(_on_bumper_charged)
	for s in [$SlingshotLeft, $SlingshotRight]:
		s.slingshot_fired.connect(_on_slingshot_fired)
	enemy.interference.connect(_on_enemy_interference)
	enemy.telegraph_started.connect(_on_enemy_telegraph)
	enemy.telegraph_cancelled.connect(func() -> void: _aim_node = null)
	_apply_chapter()
	enemy.defeated.connect(_on_enemy_defeated)
	if lanes != null:
		lanes.completed.connect(_on_lanes_completed)
	if enemy.has_signal("phase_changed"):
		enemy.phase_changed.connect(_on_enemy_phase_changed)
	if enemy.has_signal("box_lost"):
		enemy.box_lost.connect(_on_enemy_box_lost)
	if enemy.has_signal("transform_started"):
		enemy.hold_transform = phase_cutscene
		enemy.transform_started.connect(_on_enemy_transform_started)
	$UI/NetButton.pressed.connect(toggle_net)
	_apply_net()

	_restart()


func _physics_process(delta: float) -> void:
	if Input.is_action_just_pressed("toggle_net"):
		toggle_net()
	_rescue_update()
	_spring_update()

	if Input.is_action_just_pressed("reset_ball"):
		if state == State.PLAYING:
			_serve()
		elif state != State.CUTSCENE:
			_restart()
		return

	# 球飛出檯面外的保險絲（segments 牆理論上擋得住，但不賭）
	for extra in extra_balls.duplicate():
		var q: Vector2 = extra.global_position
		if q.x < -60.0 or q.x > 780.0 or q.y < -200.0 or q.y > 1400.0:
			_remove_extra(extra)
	var p := ball.global_position
	if p.x < -60.0 or p.x > 780.0 or p.y < -200.0 or p.y > 1400.0:
		_serve()
		return

	# 發射道的彈簧：按住空白鍵往下壓，放開彈回去把球打出去
	if _wait_release and not Input.is_action_pressed("launch_ball"):
		_wait_release = false
	if state == State.PLAYING and not _wait_release and Input.is_action_pressed("launch_ball"):
		if is_instance_valid(_brief):
			_brief.close()
		_charging = true
		_charge = minf(_charge + delta / charge_time, 1.0)
		_snap_t = -1.0
	elif _charging:
		_release_plunger()
	_update_plunger(delta)
	_update_kickbacks(delta)
	if _net_flash > 0.0:
		_net_flash = maxf(_net_flash - delta * 4.0, 0.0)
	queue_redraw()


# ── 測試用快捷鍵（只在編輯器／除錯版有效，網頁正式版沒有）───────

## K：立刻失手（測失敗劇情）　L：立刻打贏（快速跳到下一章）
func _unhandled_input(event: InputEvent) -> void:
	if not false or state != State.PLAYING:
		return
	var key := event as InputEventKey
	if key == null or not key.pressed or key.echo:
		return
	match key.keycode:
		KEY_K:
			debug_lose()
		KEY_L:
			debug_win()


## 測試用：剩下的球全部算掉光
func debug_lose() -> void:
	if state != State.PLAYING:
		return
	_clear_extra_balls()
	Combat.break_combo()
	Juice.play("drain", 1.0, -3.0)
	balls_left = 0
	state = State.OVER
	enemy.active = false
	_report(false)


## 測試用：敵人直接打倒（跳過第二階段）
func debug_win() -> void:
	if state != State.PLAYING or not enemy.alive:
		return
	for part in enemy.get_children():
		if part.has_method("take_damage"):
			part.take_damage(99)
	enemy.alive = false
	enemy.active = false
	enemy.defeated.emit()


## 匯出的正式版不給作弊按鈕；手機上把鍵盤說明換成觸控說明
func _setup_hud() -> void:
	var dev := false
	$UI/NetButton.visible = dev
	$UI/NetHint.visible = dev
	if dev:
		# 左上角讓給暫停鈕
		$UI/NetButton.position.x += 76.0
		$UI/NetHint.position.x += 76.0
	if DisplayServer.is_touchscreen_available():
		$UI/Hint.text = "Mitad izquierda / derecha: patitas\nMantén Lanzar para cargar el ovillo"


# ── 作弊：安全網 ────────────────────────────────────────────

func toggle_net() -> void:
	cheat_net = not cheat_net
	_apply_net()


## 網子開著嗎：作弊安全網，或是雪兒救援中
func _net_on() -> bool:
	return cheat_net or _rescue_armed


func _apply_net() -> void:
	if _net_shape != null:
		_net_shape.set_deferred("disabled", not _net_on())
	# 網子最低點落在 drain 感應區內，球停在那裡會一直觸發訊號。
	# 直接把感應關掉，比在 handler 裡濾乾淨。
	$Drain.set_deferred("monitoring", not _net_on())
	$UI/NetButton.text = "Red de seguridad: sí" if cheat_net else "Red de seguridad: no"
	queue_redraw()


## 兩段式：球先被推到中央漏球口正下方，再直直彈上去。
## 中央那條走廊（x 約 309–361）不論翼板在哪個角度都不會被擋住，
## 所以直上一定回得了檯面，不會卡在翼板底下反覆彈。
func _spring_update() -> void:
	if not _net_on():
		return
	for b in all_balls():
		_spring_ball(b)


func _spring_ball(b: RigidBody2D) -> void:
	var p := b.global_position
	if p.x > 626.0 or p.y < spring_trigger_y:
		return

	var dx := 335.0 - p.x
	if absf(dx) > 45.0:
		# 只推橫向，不要碰縱向速度。試過壓制彈跳想讓球快點滑到中央，
		# 結果球被釘在漏斗帶裡出不來，9 個測試案例掛掉 8 個。別再試了。
		b.linear_velocity.x = signf(dx) * 260.0
		return

	var now := Time.get_ticks_msec() / 1000.0
	if now < _spring_next:
		return
	_spring_next = now + 0.25
	b.linear_velocity = Vector2(dx * 2.0, 0.0)
	b.apply_central_impulse(Vector2(0.0, -spring_force))
	_net_flash = 1.0
	_rescue_used = _rescue_armed
	Juice.play("sling", 0.62, -4.0)
	Juice.burst(p, Color(0.42, 1.0, 0.70), 12, 260.0)


# ── 第二關：阿吉換階段、紙箱 ────────────────────────────────

func _on_enemy_phase_changed(phase: int) -> void:
	if sprinkler != null:
		sprinkler.set_phase(phase)


## 阿吉頭上的紙箱被打掉 → 進寶搶回頭盔，砲台恢復打 1 下就開砲
func _on_enemy_box_lost() -> void:
	for b in get_tree().get_nodes_in_group("bumpers"):
		if b.skin == "jinbao_bare":
			b.skin = "jinbao"
			b.hits_per_shot = 1
			b.reset_charge()
			Juice.number(b.global_position + Vector2(-80.0, -56.0), "¡Jinbao recupera el casco!", Color(0.95, 0.8, 0.45), 22)


# ── 敵人第一條血打完的過場（第二關阿吉變身、第三關城堡倒塌）────────

## 敵人第一條血打完：檯面暫停（球定住、爪子鎖住、敵人不攻擊），播敵人提供的過場動畫。
## 播完從發射道重新發球，不扣球
func _on_enemy_transform_started() -> void:
	if state != State.PLAYING:
		enemy.finish_transform_now()
		return
	state = State.CUTSCENE
	# 敵人翻臉了，音樂跟著換成反派那首
	Music.play("villain", 0.6)
	enemy.active = false
	enemy.cancel_attack()
	_aim_node = null
	_charging = false
	_charge = 0.0
	for shot in get_tree().get_nodes_in_group("bullets"):
		shot.queue_free()
	for fl in [$FlipperLeft, $FlipperRight]:
		fl.locked = true
	# 球在地道裡就直接回發射道（定住的球接不到地道的瞬移）；不然定在原地
	if ball.is_diving():
		ball.teleport(lane_spawn)
	else:
		ball.hold(true)
	_cutscene = PhaseCutscene.new()
	_cutscene.table = self
	_cutscene.beats = enemy.cutscene_beats(_chapter_level().get("phase2_cutscene", {}))
	_cutscene.finished.connect(_on_cutscene_finished)
	add_child.call_deferred(_cutscene)


func _on_cutscene_finished() -> void:
	_cutscene = null
	_end_cutscene()
	_play_level_music(0.8)
	state = State.PLAYING
	enemy.active = true
	_wait_release = true
	_serve()
	# 第三關：過場播完才放第二顆球（進寶飛天撲擊）
	if enemy.has_method("after_cutscene"):
		enemy.after_cutscene(self)


## 收掉過場：刪掉過場節點（還沒播完的話）、爪子解鎖、球放開
func _end_cutscene() -> void:
	if is_instance_valid(_cutscene):
		_cutscene.queue_free()
	_cutscene = null
	for fl in [$FlipperLeft, $FlipperRight]:
		fl.locked = false
	ball.hold(false)


## 暫停／繼續。暫停時把這一關的說明叫出來，順便當「我剛剛在幹嘛」的提醒。
## 結束畫面（贏了或輸了）不給暫停，那時候正在等著換場景。
func toggle_pause() -> void:
	if state == State.OVER or state == State.WIN:
		return
	var want := not get_tree().paused
	get_tree().paused = want
	# 音樂不停，但壓低一點，讓人聽得出來遊戲停住了
	Music.duck(want)
	if want:
		_show_brief(true)
	elif is_instance_valid(_brief):
		_brief.close()


## 關卡開場的任務卡。內容在 campaign.json 的 level.brief，沒寫就不跳。
## for_pause = 暫停時叫出來的那張，不會自己收掉。
func _show_brief(for_pause := false) -> void:
	if is_instance_valid(_brief):
		_brief.queue_free()
	var cfg: Dictionary = _chapter_level().get("brief", {})
	var body: Array = cfg.get("lines", [])
	if body.is_empty():
		return
	_brief = LevelBrief.new()
	_brief.title = str(cfg.get("title", ""))
	_brief.lines = body
	_brief.paused = for_pause
	add_child(_brief)


## 這一關的音樂。用場景檔名對章節，所以單獨開關卡場景測試時也放得對。
func _play_level_music(fade := Music.FADE) -> void:
	var track := "level1"
	var chapters: Array = Flow.campaign.get("chapters", [])
	for i in chapters.size():
		var cfg: Dictionary = (chapters[i] as Dictionary).get("level", {})
		if str(cfg.get("scene", Flow.TABLE_SCENE)) == scene_file_path:
			track = "level%d" % mini(i + 1, 3)
			break
	Music.play(track, fade)


## 這一關在 campaign.json 的 level 設定。用關卡場景找章節，單獨開關卡場景時也找得到
func _chapter_level() -> Dictionary:
	for ch in Flow.campaign.get("chapters", []):
		var cfg: Dictionary = ch.get("level", {})
		if str(cfg.get("scene", Flow.TABLE_SCENE)) == scene_file_path:
			return cfg
	return {}


# ── 雪兒救援（第二關）───────────────────────────────────────

## 草叢小徑三盞全亮：雪兒待命，漏球口被彈簧封住，沒有時間限制。
## 救援中再亮滿一次，就再多救一次
func _on_lanes_completed() -> void:
	if state != State.PLAYING:
		return
	_rescue_used = false
	if not _rescue_armed:
		_rescue_armed = true
		_apply_net()
	Juice.number(Vector2(290.0, 1150.0), "¡Nieve al rescate!", Color(0.72, 0.88, 1.0), 26)
	Juice.play("launch", 1.4, -4.0)


## 救起一次球、而且球彈回 rescue_release_y 以上才收網。
## 只彈起一點點又掉下來，網子還在，會繼續接住
func _rescue_update() -> void:
	if not _rescue_armed or not _rescue_used:
		return
	if ball.global_position.y < rescue_release_y and not _in_lane():
		_rescue_armed = false
		_rescue_used = false
		_apply_net()


## 由 Flow 主導時，套用 story/campaign.json 裡這一章的關卡參數。
## 單獨開 table.tscn 或跑 dev/ 工具時不動，維持檔案裡的預設值。
func _apply_chapter() -> void:
	if not Flow.driving:
		return
	var ch := Flow.current_chapter()
	chapter_title = str(ch.get("title", ""))
	var cfg: Dictionary = ch.get("level", {})
	balls_per_game = int(cfg.get("balls", balls_per_game))
	for part in enemy.get_children():
		var key := "core_hp" if part.is_core else "limb_hp"
		part.max_hp = int(cfg.get(key, part.max_hp))


## 關卡結束，交還給 Flow。留一點時間讓玩家看到結果畫面。
func _report(won: bool) -> void:
	Music.stinger("win" if won else "lose")
	if not Flow.driving:
		return
	get_tree().create_timer(win_delay if won else lose_delay, true, false, true).timeout.connect(
		func() -> void: Flow.level_finished(won))


## 球是否還待在右側發射道裡
func _in_lane() -> bool:
	var p := ball.global_position
	return p.x > 626.0 and p.y > 900.0


func _restart() -> void:
	_end_cutscene()
	_show_brief()
	# 劇情也吃空白鍵，翻到最後一句進關卡時手指常常還壓著。
	# 不先要求放開，一進來就會自動蓄力發球，任務卡還沒看就被收掉了
	_wait_release = Input.is_action_pressed("launch_ball")
	state = State.PLAYING
	balls_left = balls_per_game
	Combat.break_combo()
	enemy.reset()
	enemy.active = true
	for b in get_tree().get_nodes_in_group("bumpers"):
		b.reset()
	for shot in get_tree().get_nodes_in_group("bullets"):
		shot.queue_free()
	for s in [$SlingshotLeft, $SlingshotRight]:
		s.enabled = true
	for fl in [$FlipperLeft, $FlipperRight]:
		fl.set_shrunk(false)
	_aim_node = null
	_clear_extra_balls()
	for i in _kick_wait.size():
		_kick_wait[i] = 0.0
		_kick_flash[i] = 0.0
	_rescue_armed = false
	_rescue_used = false
	_apply_net()
	if lanes != null:
		lanes.reset()
	_serve()


func _serve() -> void:
	_charging = false
	_charge = 0.0
	ball.skin = "yarn"
	ball.teleport(lane_spawn)


func _launch() -> void:
	ball.apply_central_impulse(Vector2(0.0, -lerpf(launch_min, launch_max, _charge)))
	Juice.play("launch", lerpf(0.85, 1.25, _charge), -8.0)
	_charging = false
	_charge = 0.0


## 放開彈簧：球還壓在擋板上就照蓄力打出去，然後擋板彈回原位
func _release_plunger() -> void:
	_snap_from = _plunger_top()
	if state == State.PLAYING and _ball_on_plunger():
		_launch()
	_snap_t = 0.0
	_charging = false
	_charge = 0.0
	# 回彈的那一下關掉碰撞：力道只由 _launch() 決定，擋板不會再額外推球，
	# 發球手感才跟原本的蓄力長條一樣
	_plunger_shape.set_deferred("disabled", true)


func _ball_on_plunger() -> bool:
	return _in_lane() and ball.global_position.y > _plunger_top() - 28.0


## 擋板頂端現在的物理高度（跟著蓄力往下壓）
func _plunger_top() -> float:
	return PLUNGER_REST + PLUNGER_TRAVEL * _charge


func _update_plunger(delta: float) -> void:
	if _snap_t >= 0.0:
		_snap_t += delta
		if _snap_t >= PLUNGER_SNAP and _plunger_shape.disabled:
			_plunger_shape.set_deferred("disabled", false)
		if _snap_t > 0.6:
			_snap_t = -1.0
	_plunger.position = Vector2(lane_spawn.x, _plunger_top() + 5.0)


## 畫出來的擋板高度：放開後快速彈回原位，再抖幾下
func _plunger_draw_top() -> float:
	if _snap_t < 0.0:
		return _plunger_top()
	if _snap_t < PLUNGER_SNAP:
		return lerpf(_snap_from, PLUNGER_REST, _snap_t / PLUNGER_SNAP)
	var t := _snap_t - PLUNGER_SNAP
	return PLUNGER_REST - sin(t * 38.0) * 5.0 * exp(-t * 9.0)


# ── 外道救球彈簧（三關共用）──────────────────────────────────

## 左右外道通道中間各放一個感應點
func _build_kickbacks() -> void:
	for i in KICKBACK_SPOTS.size():
		var area := Area2D.new()
		area.collision_layer = 0
		area.collision_mask = 2
		area.monitorable = false
		var circle := CircleShape2D.new()
		circle.radius = 16.0
		var col := CollisionShape2D.new()
		col.shape = circle
		area.add_child(col)
		area.position = KICKBACK_SPOTS[i]
		area.body_entered.connect(_on_kickback_entered.bind(i))
		add_child(area)


## 球往下掉進外道：彈簧充好了就把球踢回檯面，然後重新充電
func _on_kickback_entered(body: Node2D, i: int) -> void:
	if state != State.PLAYING or _kick_wait[i] > 0.0 or not (body is RigidBody2D):
		return
	var b := body as RigidBody2D
	# 往上跑經過的球（例如剛被踢回去的）不算
	if b.linear_velocity.y < 0.0:
		return
	b.linear_velocity = KICKBACK_DIRS[i].normalized() * kickback_speed
	_kick_wait[i] = kickback_cooldown
	_kick_flash[i] = 1.0
	Combat.report_hit(b.global_position, 0, "sling")
	Juice.play("launch", 1.3, -4.0)
	Juice.number(KICKBACK_SPOTS[i] + Vector2(-44.0 if i == 0 else -64.0, -62.0), "¡Ovillo salvado!",
		Color(0.55, 1.0, 0.65), 20)


func _update_kickbacks(delta: float) -> void:
	for i in _kick_wait.size():
		if _kick_wait[i] > 0.0:
			_kick_wait[i] = maxf(_kick_wait[i] - delta, 0.0)
			if _kick_wait[i] == 0.0:
				Juice.play("hit", 1.8, -10.0)
		_kick_flash[i] = maxf(_kick_flash[i] - delta * 3.0, 0.0)


# ── 雙球（第三關）───────────────────────────────────────────

## 場上所有的球（主球在第一個）
func all_balls() -> Array:
	return [ball] + extra_balls


## 多一顆球從發射道衝出去。skin 是外觀：jinbao = 進寶縮成一顆球
func spawn_extra_ball(skin := "jinbao") -> RigidBody2D:
	var b: RigidBody2D = BALL_SCENE.instantiate()
	b.skin = skin
	b.position = lane_spawn
	add_child(b)
	b.teleport(lane_spawn, Vector2(0.0, -launch_max))
	extra_balls.append(b)
	Juice.play("launch", 1.2, -4.0)
	return b


func _remove_extra(b: Node) -> void:
	extra_balls.erase(b)
	if is_instance_valid(b):
		b.queue_free()


func _clear_extra_balls() -> void:
	for b in extra_balls:
		if is_instance_valid(b):
			b.queue_free()
	extra_balls.clear()


# ── 訊號 ────────────────────────────────────────────────────

func _on_drain_body_entered(body: Node2D) -> void:
	if state != State.PLAYING or _net_on():
		return
	# 雙球：額外的球掉下去不扣命
	if body in extra_balls:
		_remove_extra(body)
		Juice.play("drain", 1.3, -9.0)
		return
	if body != ball:
		return
	# 主球掉了但場上還有別的球：主球接手那顆球的位置、速度和外觀，不扣命
	if not extra_balls.is_empty():
		var other: RigidBody2D = extra_balls.pop_front()
		ball.teleport(other.global_position, other.linear_velocity)
		ball.skin = other.skin
		other.queue_free()
		Juice.play("drain", 1.3, -9.0)
		return
	Combat.break_combo()
	Juice.play("drain", 1.0, -3.0)
	Juice.shake(Juice.shake_drain)
	balls_left -= 1
	if balls_left <= 0:
		balls_left = 0
		state = State.OVER
		enemy.active = false
		_report(false)
	_serve()


func _on_bumper_hit(_bumper, hit_ball: Node2D) -> void:
	Combat.report_hit(hit_ball.global_position, 0, "bumper")


## Bumper 蓄滿就對敵人射一發追蹤彈。
## 這是為了消滅「球在上半場繞了十秒卻毫無進度」的死時間 ——
## 玩家就算一時打不到敵人本體，也一直在推進。
func _on_bumper_charged(bumper) -> void:
	if state != State.PLAYING or not enemy.alive:
		return
	var target := enemy.nearest_target(bumper.global_position) as Node2D
	if target == null:
		# 現在沒有東西可打（第二關阿吉被困住、頭上還戴著紙箱時就會這樣）。
		# 充好的能不要白白歸零 —— 不然玩家撞了砲台卻毫無反應，會以為壞掉了
		bumper.charge = maxi(bumper.shots_needed() - 1, 0)
		bumper.queue_redraw()
		_say_no_target(bumper)
		return
	var shot := Bullet.new()
	shot.target = target
	shot.enemy = enemy
	shot.position = bumper.position
	shot.z_index = 5
	add_child(shot)
	Juice.play("sling", 1.6, -12.0)


## 砲台沒得打的時候講一聲，順便說該怎麼辦。
## 節流過，不然球在網子上連撞會洗版
func _say_no_target(bumper) -> void:
	var now := Time.get_ticks_msec() / 1000.0
	if now < _no_target_at:
		return
	_no_target_at = now + 2.5
	var hint := "¡No hay ningún objetivo disponible!"
	if enemy.has_method("no_target_hint"):
		hint = enemy.no_target_hint()
	Juice.number(bumper.global_position + Vector2(-76.0, -60.0), hint,
		Color(1.0, 0.82, 0.45), 21)


func _on_slingshot_fired(_sling, _hit_ball, pos: Vector2) -> void:
	Combat.report_hit(pos, 0, "sling")


## 預警開始，記下牠要打哪裡，畫一條瞄準線出來。
## 玩家要能在被打之前就知道「牠瞄我的左翼板」，否則預警等於沒有。
func _on_enemy_telegraph(target: String) -> void:
	_aim_node = get_node_or_null(NodePath(target))
	if _aim_node == null:
		return
	# 光一條紅線，玩家只會看到爪子突然縮掉，不知道是誰幹的。
	# 在敵人頭上講明白牠要打哪裡
	Juice.number(enemy.global_position + Vector2(-84.0, -152.0),
		"¡Apunta a %s!" % _part_name(target), Color(1.0, 0.45, 0.45), 24)


## 攻擊目標的中文名字，用來組提示字
func _part_name(target: String) -> String:
	match target:
		"FlipperLeft":
			return "la pata izquierda"
		"FlipperRight":
			return "la pata derecha"
		"SlingshotLeft":
			return "el resorte izquierdo"
		"SlingshotRight":
			return "el resorte derecho"
	return "ti"


## 敵人放招：還在的松鼠朝目標丟橡實，砸中（Acorn.FLIGHT 秒後）才生效。
##   翼板   -> 可樂娜痛得把爪子縮回去 3 秒，漏球口從 52px 拉開到 110px 以上
##   彈射器 -> 卡住 6 秒，那側檯面變安靜
func _on_enemy_interference(target: String, duration: float) -> void:
	_aim_node = null
	var node = get_node_or_null(NodePath(target))
	if node == null:
		return

	for from in enemy.throw_origins():
		var nut := Acorn.new()
		nut.style = enemy.projectile
		nut.from = from
		nut.to = node.global_position
		nut.z_index = 30
		add_child(nut)
	Juice.play("attack", 0.75, -1.0)
	get_tree().create_timer(Acorn.FLIGHT).timeout.connect(
		_on_acorn_landed.bind(node, target, duration))


## 第三關的小刺蝟滾到砲台上：雪兒／進寶被刺纏住、無法順利行動，seconds 秒內要打 2 下才開砲
func prick_turret(target: String, seconds: float) -> void:
	var node = get_node_or_null(NodePath(target))
	if node == null or state != State.PLAYING or not node.has_method("prick"):
		return
	node.prick(seconds)
	Juice.shake(10.0)
	Juice.play("clank", 0.8, -4.0)
	Juice.number(node.global_position + Vector2(-52.0, -58.0), "¡Las púas me han atascado!", Color(1.0, 0.45, 0.45), 22)


func _on_acorn_landed(node, target: String, duration: float) -> void:
	# 橡實還在飛的時候關卡就結束了：不再生效
	if not is_instance_valid(node) or state != State.PLAYING:
		return
	Juice.shake(16.0)
	Juice.burst(node.global_position, Color(0.66, 0.42, 0.24), 18, 320.0)

	if target.begins_with("Flipper"):
		node.set_shrunk(true)
		Juice.number(node.global_position + Vector2(-70.0, -46.0),
			"¡Ay! Se encoge %s" % _part_name(target), Color(1.0, 0.4, 0.45), 24)
		get_tree().create_timer(duration).timeout.connect(
			func() -> void:
				if is_instance_valid(node):
					node.set_shrunk(false)
		)
	else:
		node.enabled = false
		Juice.number(node.global_position + Vector2(-62.0, -20.0),
			"Se atasca %s" % _part_name(target), Color(1.0, 0.4, 0.4), 22)
		get_tree().create_timer(duration).timeout.connect(
			func() -> void:
				if is_instance_valid(node):
					node.enabled = true
		)


func _on_enemy_defeated() -> void:
	state = State.WIN
	Juice.hitstop(0.25)
	Juice.shake(26.0)
	add_child(Confetti.new())
	_report(true)


# ── 建構 ────────────────────────────────────────────────────

## 發射道底部的彈簧擋板。球停在擋板上，按住空白鍵往下壓、放開彈回去。
func _build_plunger() -> void:
	_plunger = AnimatableBody2D.new()
	_plunger.collision_layer = 1
	_plunger.collision_mask = 2
	var mat := PhysicsMaterial.new()
	mat.bounce = 0.0
	mat.friction = 0.6
	_plunger.physics_material_override = mat
	var shape := RectangleShape2D.new()
	shape.size = Vector2(PLUNGER_WIDTH, 10.0)
	_plunger_shape = CollisionShape2D.new()
	_plunger_shape.shape = shape
	_plunger.add_child(_plunger_shape)
	_plunger.position = Vector2(lane_spawn.x, PLUNGER_REST + 5.0)
	add_child(_plunger)


func _build_walls() -> void:
	var segments := PackedVector2Array()
	var lines := [WALL_MAIN, WALL_RIGHT_LOWER, WALL_LEFT_DIVIDER, WALL_RIGHT_DIVIDER]
	for i in lines.size():
		var line = lines[i]
		for j in range(line.size() - 1):
			segments.append(line[j])
			segments.append(line[j + 1])

		# 兩層線畫成貼圖風：底下粗的輪廓色，上面填色。
		# 牆一定由程式照碰撞線畫，檯面底圖（art/playfield.png）不畫牆。
		var inner := CatArt.WALL_FILL if i < 2 else CatArt.ISLAND_FILL
		for pass_i in 2:
			var view := Line2D.new()
			view.points = PackedVector2Array(line)
			view.width = WALL_WIDTH + (9.0 if pass_i == 0 else 0.0)
			view.default_color = CatArt.INK if pass_i == 0 else inner
			view.joint_mode = Line2D.LINE_JOINT_ROUND
			view.begin_cap_mode = Line2D.LINE_CAP_ROUND
			view.end_cap_mode = Line2D.LINE_CAP_ROUND
			view.z_index = -20 + pass_i
			add_child(view)

	var shape := ConcavePolygonShape2D.new()
	shape.segments = segments
	var col := CollisionShape2D.new()
	col.shape = shape
	$Walls.add_child(col)

	var net := ConcavePolygonShape2D.new()
	net.segments = PackedVector2Array([
		NET_LINE[0], NET_LINE[1], NET_LINE[1], NET_LINE[2]
	])
	_net_shape = CollisionShape2D.new()
	_net_shape.shape = net
	_net_shape.disabled = true
	$SafetyNet.add_child(_net_shape)


# ── HUD（刻意只用畫的，Godot 內建字型沒有中文字符）──────────────

func _draw() -> void:
	CatArt.plunger(self, lane_spawn.x, _plunger_draw_top(), PLUNGER_FLOOR, _charge)
	for i in KICKBACK_SPOTS.size():
		CatArt.kickback(self, KICKBACK_SPOTS[i], _kick_wait[i] <= 0.0,
			1.0 - _kick_wait[i] / maxf(kickback_cooldown, 0.01), _kick_flash[i])

	# 剩餘球數 = 血量（設計文件 3.2：不另設 HP 條）
	for i in balls_per_game:
		CatArt.yarn_icon(self, Vector2(60.0 + i * 28.0, 1258.0), 10.0, i < balls_left)

	# 預警瞄準線：牠要打哪裡，現在就講清楚
	if _aim_node != null and is_instance_valid(_aim_node):
		var pulse: float = 0.45 + 0.35 * sin(Time.get_ticks_msec() / 60.0)
		var b: Vector2 = _aim_node.global_position
		for from in enemy.throw_origins():
			CatArt.attack_aim(self, from, b, pulse)

	if _net_on():
		_draw_spring()

	var font := ThemeDB.fallback_font
	if Combat.combo >= 3:
		var combo := "Combo x%d" % Combat.combo
		draw_string_outline(font, Vector2(200.0, 1266.0), combo,
			HORIZONTAL_ALIGNMENT_LEFT, -1, 20, 6, CatArt.INK)
		draw_string(font, Vector2(200.0, 1266.0), combo,
			HORIZONTAL_ALIGNMENT_LEFT, -1, 20, Color(1.0, 0.86, 0.4))

	if chapter_title != "":
		draw_string(font, Vector2(0.0, 42.0), chapter_title,
			HORIZONTAL_ALIGNMENT_CENTER, 720, 22, Color(0.973, 0.882, 0.714, 0.9))
	if false:
		draw_string(font, Vector2(18.0, 92.0), "",
			HORIZONTAL_ALIGNMENT_LEFT, -1, 13, Color(CatArt.INK, 0.6))

	match state:
		State.OVER:
			_banner(font, "Se acabaron los ovillos", "Vamos otra vez…" if Flow.driving else "R: volver a empezar",
				Color(1.0, 0.42, 0.42))
		State.WIN:
			_banner(font, "¡Conseguido!", "…" if Flow.driving else "R: volver a empezar",
				Color(0.55, 1.0, 0.65))


func _banner(font: Font, title: String, sub: String, col: Color) -> void:
	draw_rect(Rect2(0.0, 560.0, 720.0, 160.0), Color(0.03, 0.03, 0.05, 0.82))
	draw_string(font, Vector2(0.0, 640.0), title, HORIZONTAL_ALIGNMENT_CENTER, 720, 52, col)
	draw_string(font, Vector2(0.0, 686.0), sub, HORIZONTAL_ALIGNMENT_CENTER, 720, 22,
		Color(0.72, 0.75, 0.85))


## 把安全網畫成一條彈簧（鋸齒），讓玩家一眼看出作弊模式開著。
func _draw_spring() -> void:
	# 作弊安全網是橘色，雪兒救援是雪兒的淺藍色
	var base := Color(0.878, 0.604, 0.353) if cheat_net else Color(0.56, 0.78, 0.95)
	var col := base.lerp(Color(1, 1, 1), _net_flash)
	for i in NET_LINE.size() - 1:
		var a := NET_LINE[i]
		var b := NET_LINE[i + 1]
		var n := (b - a).orthogonal().normalized() * (7.0 + 5.0 * _net_flash)
		var steps := 22
		var pts := PackedVector2Array()
		for j in steps + 1:
			var t := float(j) / float(steps)
			pts.append(a.lerp(b, t) + n * (1.0 if j % 2 == 0 else -1.0))
		draw_polyline(pts, col, 3.0)
	draw_line(NET_LINE[0], NET_LINE[1], Color(col, 0.35), 2.0)
	draw_line(NET_LINE[1], NET_LINE[2], Color(col, 0.35), 2.0)
	if _rescue_armed and not cheat_net:
		var label := "Nieve te cubre"
		var font := ThemeDB.fallback_font
		draw_string_outline(font, Vector2(0.0, 1212.0), label, HORIZONTAL_ALIGNMENT_CENTER, 670, 18, 5, CatArt.INK)
		draw_string(font, Vector2(0.0, 1212.0), label, HORIZONTAL_ALIGNMENT_CENTER, 670, 18, Color(0.82, 0.93, 1.0))
