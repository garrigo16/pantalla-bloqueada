extends Control
## 開始畫面。網頁版一定要有：瀏覽器要玩家先點一下，才允許播聲音。
## 順便當標題畫面：點一下（或按任意鍵）就開始第一章。

const CREAM := Color(1.0, 0.97, 0.9)

var _t := 0.0
var _starting := false


func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	# Control 預設會在 GUI 層攔下滑鼠／觸控，_unhandled_input 就收不到了
	mouse_filter = Control.MOUSE_FILTER_IGNORE


func _process(delta: float) -> void:
	_t += delta
	queue_redraw()


func _unhandled_input(event: InputEvent) -> void:
	if _starting:
		return
	var go := false
	if event is InputEventMouseButton and (event as InputEventMouseButton).pressed:
		go = true
	elif event is InputEventScreenTouch and (event as InputEventScreenTouch).pressed:
		go = true
	elif event is InputEventKey and (event as InputEventKey).pressed and not (event as InputEventKey).echo:
		go = true
	if go:
		_start()


func _start() -> void:
	_starting = true
	Juice.play("launch", 1.1, -4.0)
	# 瀏覽器就是靠這一下點擊解鎖音訊，音樂從這裡開始最順
	Music.play("theme", 0.6)
	Flow.start()


func _draw() -> void:
	var size := get_viewport_rect().size
	draw_rect(Rect2(Vector2.ZERO, size), CREAM)
	var font := ThemeDB.fallback_font
	var mid := size * 0.5

	# 標題
	draw_string_outline(font, Vector2(0.0, mid.y - 170.0), "La defensa de los gatos",
		HORIZONTAL_ALIGNMENT_CENTER, size.x, 54, 10, CatArt.INK)
	draw_string(font, Vector2(0.0, mid.y - 170.0), "La defensa de los gatos",
		HORIZONTAL_ALIGNMENT_CENTER, size.x, 54, Color(1.0, 0.78, 0.35))
	draw_string(font, Vector2(0.0, mid.y - 124.0), "Cat Warm House · jueyi0206",
		HORIZONTAL_ALIGNMENT_CENTER, size.x, 22, Color(CatArt.INK, 0.75))

	# 中間放一顆毛線球（就是遊戲裡的球）
	CatArt.yarn_ball(self, 34.0, mid + Vector2(0.0, -20.0))

	# 「點一下開始」：一閃一閃
	var blink := 0.55 + 0.45 * sin(_t * 3.0)
	draw_string(font, Vector2(0.0, mid.y + 96.0), "Pulsa Espacio para empezar",
		HORIZONTAL_ALIGNMENT_CENTER, size.x, 28, Color(CatArt.INK, blink))

	# 操作說明
	var lines := [
		"Colona, Nieve y Jinbao te necesitan",
		"Dos Ctrl: patitas · Mantén Espacio para lanzar",
	]
	for i in lines.size():
		draw_string(font, Vector2(0.0, size.y - 150.0 + i * 34.0), str(lines[i]),
			HORIZONTAL_ALIGNMENT_CENTER, size.x, 17, Color(CatArt.INK, 0.7))
