class_name LevelBrief
extends Node2D
## 關卡開場的任務卡：這一關要做什麼、怎麼操作。
##
## 真人試玩的回饋是「不知道在玩什麼，但是球飛來飛去很有趣」——
## 劇情有講故事，可是沒有人告訴玩家「你現在要打什麼、按什麼」。
##
## 卡片在球還在發射道時出現，按發球就收掉（`close()`），不按也會自己淡掉，
## 所以不會卡住想直接開打的人。
##
## 內容在 story/campaign.json 的 level.brief，這裡不寫死任何一關的文字。

## 淡入、沒人理的話停多久、淡出
const FADE_IN := 0.35
const HOLD := 15.0
const FADE_OUT := 0.5
## 至少留這麼久才收得掉。
## 從劇情一路按過來的手指常常還壓著發球鍵，沒有這道保護，卡片會一閃就沒
const MIN_SHOW := 3.0

## 卡片的左緣、寬度、垂直中心。高度隨字數自己長，所以只給中心
const CARD_X := 52.0
const CARD_W := 616.0
const CARD_MID_Y := 470.0
const PAD := 34.0
const TITLE_SIZE := 36
const BODY_SIZE := 25
const HINT_SIZE := 20
const LINE_GAP := 18.0

const CREAM := Color(0.965, 0.933, 0.875)
const ACCENT := Color(0.949, 0.651, 0.353)


var title := ""
var lines: Array = []
## 暫停時叫出來的：不會自己收掉，底下那行字也不一樣
var paused := false

var _t := 0.0
var _alpha := 0.0
var _closing := false
## 有人要求關掉，但還沒滿 MIN_SHOW
var _close_wanted := false


func _ready() -> void:
	z_index = 45
	# 暫停時整棵樹都停了，這張卡要繼續淡入、繼續重畫
	process_mode = Node.PROCESS_MODE_ALWAYS
	set_process(true)


## 玩家開始蓄力了，把卡片收掉。太早收不算數，等 MIN_SHOW 到了再收。
func close() -> void:
	_close_wanted = true


func _process(delta: float) -> void:
	_t += delta
	if paused:
		# 暫停叫出來的不趕人，也不用等最短顯示時間 —— 玩家想回去就馬上回去
		_closing = _close_wanted
	elif _t >= MIN_SHOW and (_close_wanted or _t >= FADE_IN + HOLD):
		_closing = true
	if _closing:
		_alpha = maxf(_alpha - delta / FADE_OUT, 0.0)
		if _alpha <= 0.0:
			queue_free()
			return
	else:
		_alpha = minf(_alpha + delta / FADE_IN, 1.0)
	queue_redraw()


func _draw() -> void:
	if _alpha <= 0.0:
		return
	var a := _alpha
	var font := ThemeDB.fallback_font
	var inner := CARD_W - PAD * 2.0

	# 先量每一段要佔多高，卡片才知道自己該多大（draw_string 不會換行，
	# 字一多就會被切掉，所以內文一律走 multiline）
	var heights: Array[float] = []
	var body_h := 0.0
	for line in lines:
		var h: float = font.get_multiline_string_size(
			str(line), HORIZONTAL_ALIGNMENT_LEFT, inner, BODY_SIZE).y
		heights.append(h)
		body_h += h + LINE_GAP
	var card_h := 104.0 + body_h + 88.0
	var card := Rect2(CARD_X, CARD_MID_Y - card_h * 0.5, CARD_W, card_h)

	# 卡片往上飄一點點進場，比直接淡入有精神
	draw_set_transform(Vector2(0.0, (1.0 - a) * 18.0), 0.0, Vector2.ONE)
	draw_rect(card, Color(0.09, 0.07, 0.07, 0.90 * a))
	draw_rect(card, Color(CREAM, 0.85 * a), false, 3.0)

	draw_string(font, Vector2(CARD_X, card.position.y + 62.0), title,
		HORIZONTAL_ALIGNMENT_CENTER, CARD_W, TITLE_SIZE, Color(ACCENT, a))
	draw_line(Vector2(CARD_X + 40.0, card.position.y + 86.0),
		Vector2(card.end.x - 40.0, card.position.y + 86.0), Color(CREAM, 0.3 * a), 2.0)

	var y := card.position.y + 104.0 + BODY_SIZE
	for i in lines.size():
		draw_multiline_string(font, Vector2(CARD_X + PAD, y), str(lines[i]),
			HORIZONTAL_ALIGNMENT_LEFT, inner, BODY_SIZE, -1, Color(CREAM, 0.94 * a))
		y += heights[i] + LINE_GAP

	# 操作說明由程式決定，電腦和手機不一樣
	var touch := DisplayServer.is_touchscreen_available()
	var keys := "Mitad izquierda / derecha: patitas" if touch else "Ctrl izquierdo: pata izq.    Ctrl derecho: pata dcha."
	var go := "Mantén pulsado Lanzar para empezar" if touch else "Mantén Espacio y suelta para lanzar"
	if paused:
		keys = "En pausa"
		go = "Pulsa otra vez el botón para continuar" if touch else "Pulsa P para continuar"
	draw_string(font, Vector2(CARD_X, card.end.y - 56.0), keys,
		HORIZONTAL_ALIGNMENT_CENTER, CARD_W, HINT_SIZE, Color(CREAM, 0.62 * a))
	draw_string(font, Vector2(CARD_X, card.end.y - 24.0), go,
		HORIZONTAL_ALIGNMENT_CENTER, CARD_W, HINT_SIZE, Color(ACCENT, 0.8 * a))
	draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)
