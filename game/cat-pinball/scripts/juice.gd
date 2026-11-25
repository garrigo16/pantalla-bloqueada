extends Node2D
## 打擊感。只聽 Combat 的訊號，不碰任何遊戲邏輯。
##
## 設計文件 3.5：這些加起來約 200 行，貢獻的爽度超過兩個月的美術工作。

@export var camera: Camera2D

@export_group("Hitstop")
## 打到敵人部位時凍結多久（秒，實際時間）
@export var hitstop_hit := 0.045
## 打爆一個部位時凍結多久
@export var hitstop_kill := 0.11
## 凍結期間的時間流速
@export var hitstop_scale := 0.05

@export_group("Movimiento de cámara")
@export var shake_bumper := 2.5
@export var shake_hit := 7.0
@export var shake_kill := 16.0
@export var shake_drain := 5.0
## 每秒衰減多少像素
@export var shake_decay := 44.0

@export_group("Efectos de sonido")
## 每一段連擊往上幾個半音
@export var combo_semitones := 0.55
@export var pitch_max := 2.6

const SFX := {
	"hit": preload("res://audio/hit.wav"),
	"sling": preload("res://audio/sling.wav"),
	"clank": preload("res://audio/clank.wav"),
	"break": preload("res://audio/break.wav"),
	"drain": preload("res://audio/drain.wav"),
	"launch": preload("res://audio/launch.wav"),
	"attack": preload("res://audio/attack.wav"),
}

const PALETTE := {
	"bumper": Color(0.62, 0.78, 1.0),
	"sling": Color(1.0, 0.82, 0.42),
	"part": Color(1.0, 0.45, 0.42),
	"armor": Color(0.72, 0.78, 0.92),
	"core": Color(1.0, 0.32, 0.58),
}

var _shake := 0.0
var _hitstop_until := 0
var _players: Array[AudioStreamPlayer] = []
var _next_player := 0


func _ready() -> void:
	z_index = 60
	# 8 個播放器輪流用，連擊時聲音才不會互相切斷
	for i in 8:
		var p := AudioStreamPlayer.new()
		p.bus = "Master"
		add_child(p)
		_players.append(p)
	Combat.hit_resolved.connect(_on_hit_resolved)


func _process(delta: float) -> void:
	if _hitstop_until > 0 and Time.get_ticks_msec() >= _hitstop_until:
		_hitstop_until = 0
		Engine.time_scale = 1.0

	if not is_instance_valid(camera):
		camera = null
		return
	if _shake > 0.0:
		# hitstop 期間 delta 被縮到 5%，震動幾乎凍住 —— 這正是我們要的定格感
		_shake = maxf(_shake - shake_decay * delta, 0.0)
		camera.offset = Vector2(
			randf_range(-_shake, _shake), randf_range(-_shake, _shake)
		)
	elif camera.offset != Vector2.ZERO:
		camera.offset = Vector2.ZERO


# ── 對外 ────────────────────────────────────────────────────

## 換場景時呼叫：把殘留的粒子與傷害數字清掉，並解除 hitstop。
func clear() -> void:
	Engine.time_scale = 1.0
	_hitstop_until = 0
	_shake = 0.0
	camera = null
	for c in get_children():
		if c is CPUParticles2D or c is Label:
			c.queue_free()


func hitstop(seconds: float) -> void:
	Engine.time_scale = hitstop_scale
	_hitstop_until = maxi(_hitstop_until, Time.get_ticks_msec() + int(seconds * 1000.0))


func shake(amount: float) -> void:
	_shake = maxf(_shake, amount)


func play(sfx: String, pitch := 1.0, volume_db := 0.0) -> void:
	if not SFX.has(sfx):
		return
	var p := _players[_next_player]
	_next_player = (_next_player + 1) % _players.size()
	p.stream = SFX[sfx]
	p.pitch_scale = clampf(pitch, 0.4, 3.0)
	p.volume_db = volume_db
	p.play()


func burst(pos: Vector2, color: Color, amount := 14, power := 260.0) -> void:
	var p := CPUParticles2D.new()
	p.position = pos
	p.emitting = false
	p.one_shot = true
	p.explosiveness = 1.0
	p.amount = amount
	p.lifetime = 0.42
	p.direction = Vector2.UP
	p.spread = 180.0
	p.gravity = Vector2(0.0, 520.0)
	p.initial_velocity_min = power * 0.35
	p.initial_velocity_max = power
	p.scale_amount_min = 1.5
	p.scale_amount_max = 3.5
	p.damping_min = 40.0
	p.damping_max = 120.0
	var g := Gradient.new()
	g.set_color(0, color)
	g.set_color(1, Color(color.r, color.g, color.b, 0.0))
	p.color_ramp = g
	add_child(p)
	p.emitting = true
	# ignore_time_scale = true，否則 hitstop 會把清理也一起凍住
	get_tree().create_timer(p.lifetime + 0.3, true, false, true).timeout.connect(p.queue_free)


func number(pos: Vector2, text: String, color: Color, size := 26) -> void:
	var l := Label.new()
	l.text = text
	l.add_theme_font_size_override("font_size", size)
	l.add_theme_color_override("font_color", color)
	l.add_theme_constant_override("outline_size", 5)
	l.add_theme_color_override("font_outline_color", Color(0.03, 0.03, 0.05, 0.9))
	l.position = pos + Vector2(-18.0, -20.0)
	l.z_index = 100
	add_child(l)
	var tw := create_tween().set_parallel(true)
	tw.tween_property(l, "position:y", l.position.y - 52.0, 0.7).set_trans(Tween.TRANS_QUART).set_ease(Tween.EASE_OUT)
	tw.tween_property(l, "modulate:a", 0.0, 0.7).set_delay(0.16)
	tw.chain().tween_callback(l.queue_free)


# ── 訊號 ────────────────────────────────────────────────────

func _on_hit_resolved(pos: Vector2, damage: int, combo: int, killed: bool, kind: String) -> void:
	var color: Color = PALETTE.get(kind, Color.WHITE)
	var pitch := pow(2.0, (combo - 1) * combo_semitones / 12.0)
	pitch = minf(pitch, pitch_max)

	match kind:
		"bumper":
			shake(shake_bumper)
			burst(pos, color, 8, 190.0)
			play("hit", pitch, -7.0)
		"sling":
			shake(shake_bumper * 1.6)
			burst(pos, color, 10, 260.0)
			play("sling", pitch, -5.0)
		"armor":
			shake(shake_bumper * 1.4)
			burst(pos, color, 6, 170.0)
			play("clank", pitch * 0.9, -8.0)
		_:
			if killed:
				hitstop(hitstop_kill)
				shake(shake_kill)
				burst(pos, color, 34, 460.0)
				play("break", 1.0, -1.0)
			else:
				hitstop(hitstop_hit)
				shake(shake_hit)
				burst(pos, color, 16, 300.0)
				play("hit", pitch, -3.0)

	if damage > 0:
		var big := combo >= 6 or killed
		number(pos, str(damage), color, 34 if big else 24)
	if combo >= 4 and not killed:
		number(pos + Vector2(26.0, 14.0), "x%d" % combo, Color(1.0, 0.86, 0.4), 18)
