extends CanvasLayer
## 手機觸控（autoload「Touch」）。網頁版在手機上沒有鍵盤，用螢幕分區代替：
##
##   左半邊按住  → 左貓爪        右半邊按住 → 右貓爪
##   球在發射道時，下方中間會出現「發球」，按住蓄力、放開彈出
##
## 只在檯面（有 all_balls() 的場景）生效；劇情畫面本來就能用點擊翻頁。
## 沒有觸控螢幕的裝置完全不畫、也不攔輸入，電腦版照舊用鍵盤。

const LAUNCH_RECT := Rect2(250.0, 1130.0, 220.0, 130.0)
const PAW_Y := 1195.0

var _touch_action := {}
var _table = null
var _ui: Control


func _ready() -> void:
	layer = 8
	_ui = Control.new()
	_ui.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_ui.set_anchors_preset(Control.PRESET_FULL_RECT)
	_ui.draw.connect(_draw_ui)
	add_child(_ui)
	set_process(true)


func _process(_delta: float) -> void:
	var cur := get_tree().current_scene
	var table = cur if cur != null and cur.has_method("all_balls") else null
	if table != _table:
		_release_all()
		_table = table
	elif _table != null and _table.state != 0 and not _touch_action.is_empty():
		_release_all()
	_ui.visible = _table != null and DisplayServer.is_touchscreen_available()
	if _ui.visible:
		_ui.queue_redraw()


func _input(event: InputEvent) -> void:
	# state 0 = PLAYING。過場動畫、結束畫面時放手，讓觸控傳給過場的「點一下跳過」
	if _table == null or _table.state != 0 or not DisplayServer.is_touchscreen_available():
		return
	if event is InputEventScreenTouch:
		var touch := event as InputEventScreenTouch
		# 暫停鈕那一塊留給它自己，這裡攔下去手指就按不到了
		if PauseButton.RECT.grow(8.0).has_point(_to_game(touch.position)):
			return
		if touch.pressed:
			_press(touch.index, touch.position)
		else:
			_release(touch.index)
		get_viewport().set_input_as_handled()
	elif event is InputEventScreenDrag:
		var drag := event as InputEventScreenDrag
		# 手指滑過去換邊：先放開舊的再按新的
		var want := _action_at(drag.position)
		if _touch_action.get(drag.index, "") != want:
			_release(drag.index)
			_press(drag.index, drag.position)


## 這個位置算哪一個動作
func _action_at(pos: Vector2) -> String:
	var p := _to_game(pos)
	if _launch_visible() and LAUNCH_RECT.grow(30.0).has_point(p):
		return "launch_ball"
	return "flip_left" if p.x < 360.0 else "flip_right"


func _press(index: int, pos: Vector2) -> void:
	var action := _action_at(pos)
	_touch_action[index] = action
	Input.action_press(action)


func _release(index: int) -> void:
	var action: String = _touch_action.get(index, "")
	if action == "":
		return
	_touch_action.erase(index)
	# 同一個動作可能有兩根手指按著，都放開了才真的放開
	if not _touch_action.values().has(action):
		Input.action_release(action)


func _release_all() -> void:
	for action in _touch_action.values():
		Input.action_release(action)
	_touch_action.clear()


## 球還在發射道裡：才顯示發球鍵
func _launch_visible() -> bool:
	if _table == null or not is_instance_valid(_table):
		return false
	return _table.state == 0 and _table._in_lane()


## 螢幕座標 → 遊戲座標（720x1280）
func _to_game(pos: Vector2) -> Vector2:
	var win := Vector2(get_viewport().get_visible_rect().size)
	if win.x <= 0.0 or win.y <= 0.0:
		return pos
	return pos / win * Vector2(720.0, 1280.0)


func _draw_ui() -> void:
	var scale := _ui.size / Vector2(720.0, 1280.0)
	_ui.draw_set_transform(Vector2.ZERO, 0.0, scale)
	var held := func(a: String) -> bool: return _touch_action.values().has(a)
	for side in [-1.0, 1.0]:
		var at := Vector2(360.0 + side * 268.0, PAW_Y)
		var on: bool = held.call("flip_left" if side < 0.0 else "flip_right")
		_ui.draw_circle(at, 52.0, Color(CatArt.INK, 0.10 if not on else 0.18))
		_ui.draw_arc(at, 52.0, 0.0, TAU, 32, Color(CatArt.INK, 0.35), 3.0, true)
		_ui.draw_set_transform(at, 0.0, Vector2(side, 1.0) * 0.42)
		CatArt.paw(_ui, 0.0, 105.0, 11.0, on, Color(1, 1, 1, 0.85), 105.0, 1.0)
		_ui.draw_set_transform(Vector2.ZERO, 0.0, scale)
	if _launch_visible():
		var on2: bool = held.call("launch_ball")
		var box := LAUNCH_RECT
		_ui.draw_rect(box, Color(CatArt.INK, 0.12 if not on2 else 0.2))
		_ui.draw_rect(box, Color(CatArt.INK, 0.35), false, 3.0)
		var font := ThemeDB.fallback_font
		_ui.draw_string(font, box.position + Vector2(0.0, 82.0), "Mantén para lanzar",
			HORIZONTAL_ALIGNMENT_CENTER, box.size.x, 30, Color(CatArt.INK, 0.8))
	_ui.draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)
