extends Control
## 劇情播放器。台詞從 Flow.pending_lines 來，內容在 story/campaign.json。
##
## 畫面：上面一張插圖、下面文字框。台詞帶 "scene" 時換插圖，沒帶就沿用上一張。
## 插圖還沒生出來時，框裡顯示這張圖該畫什麼（campaign.json 的 illustrations.note），
## 看著劇情就知道還缺哪張。
##
## 這個場景只負責「把字一句一句放出來」，不知道任何劇情內容，
## 所以加章節、改台詞都不用動它。

## 打字機速度（字/秒）
@export var chars_per_second := 44.0

@onready var _title: Label = $Title
@onready var _art: TextureRect = $Frame/Art
@onready var _placeholder: Label = $Frame/Placeholder
@onready var _who: Label = $Box/Lines/Who
@onready var _act: Label = $Box/Lines/Act
@onready var _body: Label = $Box/Lines/Body
@onready var _hint: Label = $Box/Hint

var _lines: Array = []
var _i := -1
var _revealed := 0.0
var _illustrations: Dictionary = {}
var _scene := ""
## 結局的最後一句：停在這一頁（粉專 QR code），不要自己跳回開頭
var _ended := false
## 這張插圖可以點開的網址（campaign.json 的 illustrations.link）。
## 只在網頁版有值 —— 桌面版點 QR code 圖沒意義
var _link := ""
var _confirm_held := false


func _ready() -> void:
	# Control 預設 mouse_filter=STOP，會在 GUI 層把點擊吃掉，_unhandled_input 收不到。
	# 這個畫面沒有按鈕，整棵樹都放行，點哪裡都能翻頁（手機版靠這個）。
	_ignore_mouse(self)
	_lines = Flow.pending_lines
	_title.text = Flow.pending_title
	_title.visible = _title.text != ""
	_illustrations = Flow.campaign.get("illustrations", {})
	_placeholder.text = ""
	_advance_line()


## 換場景時把疊在畫布上的連結收掉，不然它會一直擋著那一塊
func _exit_tree() -> void:
	WebLink.hide()


func _process(delta: float) -> void:
	if _i < 0:
		return
	var total := float(_body.text.length())
	if _revealed < total:
		_revealed = minf(_revealed + chars_per_second * delta, total)
		_body.visible_characters = int(_revealed)
		_hint.text = "Espacio · Completar el texto"
		_hint.visible = true
	else:
		_body.visible_characters = -1
		_update_hint()
		_hint.visible = true


func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey:
		var key := event as InputEventKey
		if key.physical_keycode in [KEY_SPACE, KEY_ENTER, KEY_KP_ENTER]:
			if not key.pressed:
				_confirm_held = false
				return
			if _confirm_held or key.echo:
				get_viewport().set_input_as_handled()
				return
			_confirm_held = true
	if _ended and _is_restart_key(event):
		get_viewport().set_input_as_handled()
		Flow.start()
		return
	if _is_tap(event) or _is_confirm_key(event):
		# 一定要「先」標記已處理，「再」推進劇情。
		# 最後一句推進時會呼叫 Flow.story_finished() 換場景，這個節點當下就被
		# 移出場景樹，之後再呼叫 get_viewport() 會拿到 null → 腳本錯誤。
		# 在編輯器按 F5 執行時，腳本錯誤會讓除錯器暫停遊戲，
		# 看起來就是「劇情跑完卡住，進不了關卡」。
		get_viewport().set_input_as_handled()
		_on_confirm()


## 點一下／手指按一下（手機沒有鍵盤，翻頁只能靠這個）
func _is_tap(event: InputEvent) -> bool:
	return _tap_pos(event) != Vector2.INF


## 點在哪裡。沒點的話回 Vector2.INF
func _tap_pos(event: InputEvent) -> Vector2:
	if event is InputEventMouseButton and (event as InputEventMouseButton).pressed:
		return (event as InputEventMouseButton).position
	if event is InputEventScreenTouch and (event as InputEventScreenTouch).pressed:
		return (event as InputEventScreenTouch).position
	return Vector2.INF


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


## 結束畫面的「再玩一次」：R。
## 用 R 是因為結束畫面本來就要吃掉點擊和 Enter（免得有人手滑就從頭跑一次）。
func _is_restart_key(event: InputEvent) -> bool:
	if not (event is InputEventKey):
		return false
	var key := event as InputEventKey
	return key.pressed and not key.echo and key.physical_keycode == KEY_R


## 整棵子樹都不攔滑鼠／觸控
func _ignore_mouse(node: Node) -> void:
	if node is Control:
		(node as Control).mouse_filter = Control.MOUSE_FILTER_IGNORE
	for child in node.get_children():
		_ignore_mouse(child)


func _on_confirm() -> void:
	# 第一下先把整句補完，第二下才換下一句
	var total := float(_body.text.length())
	if _revealed < total:
		_revealed = total
		_body.visible_characters = -1
		_update_hint()
		_hint.visible = true
		return
	# 結局的最後一句就停在這裡：留著粉專的 QR code 給人掃，不要自己跳回開頭
	if _ended:
		return
	_advance_line()


func _advance_line() -> void:
	_i += 1
	if _i >= _lines.size():
		Flow.story_finished()
		return
	var line: Dictionary = _lines[_i]
	_who.text = str(line.get("who", ""))
	_who.visible = _who.text != ""
	var act := str(line.get("act", ""))
	_act.text = "（%s）" % act
	_act.visible = act != ""
	_body.text = str(line.get("text", ""))
	_body.visible_characters = 0
	_revealed = 0.0
	if Flow.pending_is_ending and _i == _lines.size() - 1:
		_mark_ended()
	_show_scene(str(line.get("scene", "")))
	# 擺在最後：要等 _show_scene 決定完這一頁有沒有連結，提示才講得對
	_update_hint()


## 走到結局最後一句：把「繼續」換成收尾的字，之後不再翻頁
func _mark_ended() -> void:
	_ended = true
	_hint.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_hint.add_theme_color_override("font_color", Color(0.949, 0.651, 0.353))


## 底下那行提示。有連結的那幾頁要講「圖可以點」，不然沒人知道
func _update_hint() -> void:
	var touch := DisplayServer.is_touchscreen_available()
	if _ended:
		if _link != "":
			_hint.text = "Fin de la demo · Gracias por jugar"
		else:
			_hint.text = "Fin de la aventura · R: volver a jugar"
	elif _link != "":
		# 圖被 HTML 連結蓋住了，翻頁要點文字框那一半
		_hint.text = "Pulsa la imagen para abrir · Texto para continuar" if touch else "Pulsa la imagen para abrir · Intro para continuar"
	else:
		_hint.text = "Pulsa para continuar" if touch else "Intro / Espacio: continuar"


## 換插圖。空字串或跟現在同一張就不動。
func _show_scene(id: String) -> void:
	if id == "" or id == _scene:
		return
	_scene = id
	var info: Dictionary = _illustrations.get(id, {})
	# 網頁版才掛連結：在畫布上疊一個真的 HTML <a>，手機才點得開（見 web_link.gd）
	_link = str(info.get("link", "")) if OS.has_feature("web") else ""
	if _link != "":
		WebLink.show(_link, $Frame.get_global_rect())
	else:
		WebLink.hide()
	var path := str(info.get("image", ""))
	if path != "" and ResourceLoader.exists(path):
		_art.texture = load(path)
		_placeholder.visible = false
		_art.modulate.a = 0.0
		create_tween().tween_property(_art, "modulate:a", 1.0, 0.25)
	else:
		_art.texture = null
		_placeholder.visible = true
		_placeholder.text = "Ilustración %s (pendiente)\n\n%s" % [id, str(info.get("note", ""))]
