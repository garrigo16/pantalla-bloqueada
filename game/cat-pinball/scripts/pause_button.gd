class_name PauseButton
extends CanvasLayer
## 暫停鈕。網頁、手機、桌面都要能按，所以自己畫一顆夠大的圓鈕，
## 滑鼠、手指、鍵盤（Esc／P）都收。
##
## process_mode 一定要是 ALWAYS：暫停的時候整棵樹都停了，
## 只有它還醒著，不然按下去就再也叫不醒。

signal toggled

## 鈕的位置與大小（檯面座標 720x1280）。放左上角，那裡本來就空著
const RECT := Rect2(16.0, 14.0, 58.0, 58.0)

const CREAM := Color(0.965, 0.933, 0.875)

var _ui: Control
## 按住的手指／滑鼠還沒放開，畫面上要有回饋
var _held := false


func _ready() -> void:
	layer = 9
	process_mode = Node.PROCESS_MODE_ALWAYS
	_ui = Control.new()
	_ui.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_ui.set_anchors_preset(Control.PRESET_FULL_RECT)
	_ui.draw.connect(_draw_ui)
	add_child(_ui)
	set_process(true)


func _process(_delta: float) -> void:
	_ui.queue_redraw()


func _input(event: InputEvent) -> void:
	if _is_pause_key(event):
		get_viewport().set_input_as_handled()
		toggled.emit()
		return
	var at := _press_pos(event)
	if at != Vector2.INF:
		if RECT.grow(8.0).has_point(_to_game(at)):
			get_viewport().set_input_as_handled()
			_held = true
			toggled.emit()
		return
	if _is_release(event):
		_held = false


## Esc／P。跟劇情那邊一樣比 physical_keycode——
## 網頁版的按鍵事件對不上內建的 ui_cancel
func _is_pause_key(event: InputEvent) -> bool:
	if not (event is InputEventKey):
		return false
	var key := event as InputEventKey
	if not key.pressed or key.echo:
		return false
	return key.physical_keycode in [KEY_ESCAPE, KEY_P]


func _press_pos(event: InputEvent) -> Vector2:
	if event is InputEventMouseButton and (event as InputEventMouseButton).pressed:
		return (event as InputEventMouseButton).position
	if event is InputEventScreenTouch and (event as InputEventScreenTouch).pressed:
		return (event as InputEventScreenTouch).position
	return Vector2.INF


func _is_release(event: InputEvent) -> bool:
	if event is InputEventMouseButton:
		return not (event as InputEventMouseButton).pressed
	if event is InputEventScreenTouch:
		return not (event as InputEventScreenTouch).pressed
	return false


## 螢幕座標 → 檯面座標（720x1280）
func _to_game(pos: Vector2) -> Vector2:
	var win := Vector2(get_viewport().get_visible_rect().size)
	if win.x <= 0.0 or win.y <= 0.0:
		return pos
	return pos / win * Vector2(720.0, 1280.0)


func _draw_ui() -> void:
	var scale := _ui.size / Vector2(720.0, 1280.0)
	_ui.draw_set_transform(Vector2.ZERO, 0.0, scale)
	var mid := RECT.position + RECT.size * 0.5
	var r: float = RECT.size.x * 0.5
	var paused := get_tree().paused
	_ui.draw_circle(mid, r, Color(0.09, 0.07, 0.07, 0.55 if not _held else 0.75))
	_ui.draw_arc(mid, r, 0.0, TAU, 32, Color(CREAM, 0.75), 2.5, true)
	if paused:
		# 暫停中畫「繼續」的三角形
		var t := r * 0.46
		_ui.draw_colored_polygon(PackedVector2Array([
			mid + Vector2(-t * 0.7, -t), mid + Vector2(t, 0.0), mid + Vector2(-t * 0.7, t)]),
			Color(CREAM, 0.92))
	else:
		# 遊戲中畫兩根暫停直條
		var w := r * 0.24
		var h := r * 0.52
		for side in [-1.0, 1.0]:
			_ui.draw_rect(Rect2(mid + Vector2(side * r * 0.34 - w * 0.5, -h), Vector2(w, h * 2.0)),
				Color(CREAM, 0.92))
	_ui.draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)
