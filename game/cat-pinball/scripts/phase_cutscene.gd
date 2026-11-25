class_name PhaseCutscene
extends Node2D
## 敵人第一條血打完時的過場動畫（由 table 建立）。每一段演什麼由敵人提供（cutscene_beats()）：
##   第二關：阿吉暈倒 → 鑽回地底 → 戴紙箱冒出來 → 進寶喊頭盔 → 第二回合
##   第三關：巴哥暈倒 → 城堡倒塌 → 跳下滑梯 → 進寶飛天撲擊 → 最終回合
##
## 每一段是一個 Dictionary：
##   time   這一段播幾秒（時間到自動往下）
##   who／at／text   對話框：誰說的、尾巴指向誰（"core" = 敵人本體，或檯面上的節點名稱，例如 TurretRight）
##   shout  狀聲詞（畫在敵人本體的位置）
##   title／sub      橫幅：大標題與下面一行提示
##   enter  這一段開始時要做的事（Callable，例如城堡倒塌、換第二條血）
##
## 過場期間檯面暫停（球定住、爪子鎖住、敵人不攻擊，由 table 處理）。
## Enter／空白鍵跳到下一段。播完送出 finished，table 從發射道重新發球。

signal finished()

const NAME_COLORS := {
	"Aji, el topo": Color(0.55, 0.36, 0.25),
	"Jefe Carlino": Color(0.78, 0.25, 0.22),
	"Jinbao": Color(0.80, 0.55, 0.16),
}
## 每段至少播多久才能按鍵跳過（避免一次連按跳掉兩段）
const MIN_BEAT := 0.25
## 台詞一秒冒出幾個字
const TEXT_SPEED := 36.0
const BUBBLE_WIDTH := 320.0
const TEXT_SIZE := 22
const NAME_SIZE := 17
const CREAM := Color(1.0, 0.97, 0.9)
const GOLD := Color(1.0, 0.78, 0.35)

## 由 table 在 add_child 之前設定
var table
var beats: Array = []

var _i := -1
var _t := 0.0
var _dim: DimLayer
## [節點, 原本的 z_index]：過場時拉到暗幕上面，結束時還原
var _raised: Array = []
var _camera: Camera2D
var _cam_pos := Vector2.ZERO
var _cam_zoom := Vector2.ONE
var _camera_returned := false


## 暗幕：蓋住檯面，敵人和說話的角色被拉到它上面
class DimLayer extends Node2D:
	var amount := 0.0

	func _draw() -> void:
		draw_rect(Rect2(-200.0, -200.0, 1120.0, 1680.0), Color(0.05, 0.04, 0.06, 0.45 * amount))


func _ready() -> void:
	z_index = 90
	_dim = DimLayer.new()
	_dim.z_as_relative = false
	# 在 Juice（60，粒子和傷害數字）底下，碎屑才看得到
	_dim.z_index = 55
	add_child(_dim)
	var raise: Array = [table.enemy]
	for b in beats:
		var at := str(b.get("at", "core"))
		if at != "core":
			var n = table.get_node_or_null(at)
			if n != null and not raise.has(n):
				raise.append(n)
	for n in raise:
		_raised.append([n, n.z_index])
		n.z_index = 58
	# 鏡頭稍微拉近敵人
	_camera = table.get_node_or_null("Camera2D")
	if _camera != null:
		_cam_pos = _camera.position
		_cam_zoom = _camera.zoom
		var to: Vector2 = _cam_pos + ((table.enemy.core_position() - _cam_pos) * 0.5).clamp(
			Vector2(-30.0, -55.0), Vector2(30.0, 55.0))
		var tw := create_tween().set_parallel(true).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_OUT)
		tw.tween_property(_camera, "zoom", _cam_zoom * 1.1, 0.5)
		tw.tween_property(_camera, "position", to, 0.5)
	Juice.play("break", 0.8, -3.0)
	advance()


func _process(delta: float) -> void:
	if _i < 0 or _i >= beats.size():
		return
	_t += delta
	_dim.amount = minf(_dim.amount + delta * 3.0, 1.0)
	_dim.queue_redraw()
	if _t >= float(beats[_i]["time"]):
		advance()
	queue_redraw()


func _unhandled_input(event: InputEvent) -> void:
	if _t < MIN_BEAT:
		return
	# 手機沒有鍵盤，點畫面任何地方都算跳過
	if _is_tap(event) or _is_confirm_key(event):
		get_viewport().set_input_as_handled()
		advance()


## 點一下／手指按一下（手機沒有鍵盤，只能靠這個跳過）
func _is_tap(event: InputEvent) -> bool:
	if event is InputEventMouseButton:
		return (event as InputEventMouseButton).pressed
	if event is InputEventScreenTouch:
		return (event as InputEventScreenTouch).pressed
	return false


## Enter／空白鍵。
## 網頁版的按鍵事件只帶 physical_keycode，keycode 是 0，
## 而內建的 ui_accept 綁的是 keycode，所以在瀏覽器裡永遠對不上 —— 這裡自己比一次。
func _is_confirm_key(event: InputEvent) -> bool:
	if not (event is InputEventKey):
		return false
	var key := event as InputEventKey
	if not key.pressed or key.echo:
		return false
	return event.is_action_pressed("ui_accept") \
		or key.physical_keycode in [KEY_ENTER, KEY_KP_ENTER, KEY_SPACE]


## 跳到下一段；最後一段之後結束過場
func advance() -> void:
	if _i >= beats.size():
		return
	_i += 1
	_t = 0.0
	if _i >= beats.size():
		_finish()
		return
	var enter: Variant = beats[_i].get("enter")
	if enter is Callable:
		(enter as Callable).call()
	queue_redraw()


func _finish() -> void:
	# 鏡頭慢慢拉回來（tween 掛在鏡頭上，這個節點刪掉也會繼續跑）
	if is_instance_valid(_camera):
		var tw := _camera.create_tween().set_parallel(true).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_OUT)
		tw.tween_property(_camera, "zoom", _cam_zoom, 0.3)
		tw.tween_property(_camera, "position", _cam_pos, 0.3)
		_camera_returned = true
	finished.emit()
	queue_free()


## 結束或被中途刪掉（例如重開一局）時，把圖層和鏡頭還原
func _exit_tree() -> void:
	for pair in _raised:
		if is_instance_valid(pair[0]):
			pair[0].z_index = pair[1]
	if is_instance_valid(_camera) and not _camera_returned:
		_camera.zoom = _cam_zoom
		_camera.position = _cam_pos


# ── 畫面 ────────────────────────────────────────────────────

func _draw() -> void:
	if _i < 0 or _i >= beats.size():
		return
	var b: Dictionary = beats[_i]
	var font := ThemeDB.fallback_font
	var pop := _ease_back(clampf(_t / 0.2, 0.0, 1.0))
	if b.has("text"):
		_bubble(font, _anchor(str(b.get("at", "core"))), str(b.get("who", "")), str(b["text"]), pop)
	elif b.has("shout"):
		_shout(font, str(b["shout"]), pop)
	elif b.has("title"):
		_title_band(font, str(b["title"]), str(b.get("sub", "")))
	var view := _view()
	var hint := "Pulsa para saltar" if DisplayServer.is_touchscreen_available() else "Intro / Espacio: saltar"
	var at := Vector2(view.position.x, view.end.y - 18.0)
	draw_string_outline(font, at, hint, HORIZONTAL_ALIGNMENT_RIGHT, view.size.x - 20.0, 15, 4, CatArt.INK)
	draw_string(font, at, hint, HORIZONTAL_ALIGNMENT_RIGHT, view.size.x - 20.0, 15, CREAM)


## 對話框：尾巴指向說話的角色，框從尾巴那一點彈出來，字一個一個冒出來
func _bubble(font: Font, anchor: Vector2, who: String, text: String, pop: float) -> void:
	var view := _view()
	var pad := Vector2(18.0, 12.0)
	var body := font.get_multiline_string_size(text, HORIZONTAL_ALIGNMENT_LEFT, BUBBLE_WIDTH, TEXT_SIZE)
	var name_h := float(NAME_SIZE) + 6.0
	var size := Vector2(maxf(body.x, 120.0), body.y + name_h) + pad * 2.0
	var pos := Vector2(anchor.x - size.x * 0.5, anchor.y - 16.0 - size.y)
	pos.x = clampf(pos.x, view.position.x + 12.0, view.end.x - 12.0 - size.x)
	pos.y = maxf(pos.y, view.position.y + 12.0)
	var box := Rect2(pos - anchor, size)

	draw_set_transform(anchor, 0.0, Vector2.ONE * pop)
	var tail_x := clampf(0.0, box.position.x + 26.0, box.end.x - 26.0)
	draw_colored_polygon(PackedVector2Array([
		Vector2(tail_x - 18.0, box.end.y - 2.0), Vector2(tail_x + 18.0, box.end.y - 2.0), Vector2(0.0, 5.0)]),
		CatArt.INK)
	var sb := StyleBoxFlat.new()
	sb.bg_color = CREAM
	sb.border_color = CatArt.INK
	sb.set_border_width_all(4)
	sb.set_corner_radius_all(16)
	draw_style_box(sb, box)
	draw_colored_polygon(PackedVector2Array([
		Vector2(tail_x - 13.0, box.end.y - 2.0), Vector2(tail_x + 13.0, box.end.y - 2.0), Vector2.ZERO]),
		CREAM)
	draw_string(font, box.position + pad + Vector2(0.0, font.get_ascent(NAME_SIZE)), who,
		HORIZONTAL_ALIGNMENT_LEFT, -1, NAME_SIZE, NAME_COLORS.get(who, CatArt.INK))
	var shown := text.substr(0, int(_t * TEXT_SPEED))
	draw_multiline_string(font, box.position + pad + Vector2(0.0, name_h + font.get_ascent(TEXT_SIZE)),
		shown, HORIZONTAL_ALIGNMENT_LEFT, BUBBLE_WIDTH, TEXT_SIZE, -1, CatArt.INK)
	draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)


## 狀聲詞：在敵人本體的位置抖動著放大出現
func _shout(font: Font, text: String, pop: float) -> void:
	var view := _view()
	var at: Vector2 = table.enemy.core_position()
	at.x = clampf(at.x, view.position.x + 170.0, view.end.x - 170.0)
	at += Vector2(randf_range(-3.0, 3.0), randf_range(-3.0, 3.0))
	draw_set_transform(at, -0.06, Vector2.ONE * pop)
	draw_string_outline(font, Vector2(-320.0, 14.0), text, HORIZONTAL_ALIGNMENT_CENTER, 640.0, 44, 12, CatArt.INK)
	draw_string(font, Vector2(-320.0, 14.0), text, HORIZONTAL_ALIGNMENT_CENTER, 640.0, 44, GOLD)
	draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)


## 橫幅：從左邊滑進來，下面一行寫這一回合要怎麼打
func _title_band(font: Font, title: String, sub: String) -> void:
	var view := _view()
	var slide := 1.0 - pow(1.0 - clampf(_t / 0.25, 0.0, 1.0), 3.0)
	var band := Rect2(view.position.x - (1.0 - slide) * view.size.x, view.get_center().y - 75.0,
		view.size.x, 150.0)
	draw_rect(band, Color(0.03, 0.03, 0.05, 0.85))
	draw_string_outline(font, band.position + Vector2(0.0, 78.0), title, HORIZONTAL_ALIGNMENT_CENTER,
		band.size.x, 54, 10, CatArt.INK)
	draw_string(font, band.position + Vector2(0.0, 78.0), title, HORIZONTAL_ALIGNMENT_CENTER,
		band.size.x, 54, GOLD)
	draw_string(font, band.position + Vector2(0.0, 122.0), sub, HORIZONTAL_ALIGNMENT_CENTER,
		band.size.x, 22, CREAM)


## 鏡頭現在看到的範圍（鏡頭拉近時，提示和橫幅要跟著畫在畫面裡）
func _view() -> Rect2:
	var size := Vector2(720.0, 1280.0)
	if not is_instance_valid(_camera):
		return Rect2(Vector2.ZERO, size)
	size /= _camera.zoom
	return Rect2(_camera.get_screen_center_position() - size * 0.5, size)


## 對話框尾巴指的位置：角色頭頂上方
func _anchor(key: String) -> Vector2:
	if key != "core":
		var n := table.get_node_or_null(key) as Node2D
		if n != null:
			return n.global_position + Vector2(0.0, -38.0)
	var core = table.enemy.get_node_or_null("Core")
	var r: float = core.radius if core != null else 30.0
	return table.enemy.core_position() + Vector2(0.0, -r - 28.0)


static func _ease_back(x: float) -> float:
	var c1 := 1.70158
	return 1.0 + (c1 + 1.0) * pow(x - 1.0, 3.0) + c1 * pow(x - 1.0, 2.0)
