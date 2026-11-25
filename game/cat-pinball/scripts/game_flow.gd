extends Node
## 遊戲流程（autoload 名稱：Flow）。
##
##   章節前劇情 → 關卡 → 章節後劇情 → 下一章 → ... → 結局
##                   ↑        │
##                   └ 失敗劇情 ┘   （敵人得意洋洋的一段，這一章沒寫 lose 就直接重打）
##
## **章節內容全部在 story/campaign.json，改劇情不用碰任何程式碼。**
## 想加一關就在那個檔案的 chapters 陣列裡多加一筆。

const CAMPAIGN_PATH := "res://story/campaign.json"
const STORY_SCENE := "res://scenes/story.tscn"
const TABLE_SCENE := "res://scenes/table.tscn"

enum Beat { INTRO, LEVEL, OUTRO, ENDING, LOSE }

## 是否由 Flow 主導流程。
## 單獨開 table.tscn 或跑 dev/ 工具時是 false，檯面會維持自己重開的舊行為。
var driving := false

var campaign: Dictionary = {}
var chapter_index := 0
var beat: Beat = Beat.INTRO

## 給 story.tscn 讀的
var pending_lines: Array = []
var pending_title := ""
var pending_is_ending := false


func _ready() -> void:
	_load_campaign()


func _load_campaign() -> void:
	var f := FileAccess.open(CAMPAIGN_PATH, FileAccess.READ)
	if f == null:
		push_error("No se encuentra %s" % CAMPAIGN_PATH)
		campaign = {"chapters": [], "ending": []}
		return
	var parsed: Variant = JSON.parse_string(f.get_as_text())
	f.close()
	if typeof(parsed) != TYPE_DICTIONARY:
		push_error("%s no contiene un objeto JSON válido" % CAMPAIGN_PATH)
		campaign = {"chapters": [], "ending": []}
		return
	campaign = parsed
	# 還在製作中的章節（"draft": true）先不進流程：劇情可以先寫好放著，不影響現在能玩的部分
	var chapters: Array = campaign.get("chapters", [])
	campaign["chapters"] = chapters.filter(func(ch): return not ch.get("draft", false))


func chapter_count() -> int:
	return (campaign.get("chapters", []) as Array).size()


func current_chapter() -> Dictionary:
	var list: Array = campaign.get("chapters", [])
	if chapter_index < 0 or chapter_index >= list.size():
		return {}
	return list[chapter_index]


# ── 流程 ────────────────────────────────────────────────────

func start() -> void:
	driving = true
	chapter_index = 0
	_go_intro()


func _go_intro() -> void:
	var ch := current_chapter()
	if ch.is_empty():
		_go_ending()
		return
	beat = Beat.INTRO
	Music.play("theme")
	pending_title = ch.get("title", "")
	pending_lines = ch.get("intro", [])
	pending_is_ending = false
	if pending_lines.is_empty():
		_go_level()
		return
	_change(STORY_SCENE)


func _go_level() -> void:
	beat = Beat.LEVEL
	Music.play("level%d" % mini(chapter_index + 1, 3))
	# 每一章可以有自己的關卡場景（campaign.json 的 level.scene），沒寫就用第一關的檯面
	var cfg: Dictionary = current_chapter().get("level", {})
	_change(str(cfg.get("scene", TABLE_SCENE)))


func _go_outro() -> void:
	var ch := current_chapter()
	beat = Beat.OUTRO
	Music.play("theme")
	pending_title = ch.get("outro_title", "")
	pending_lines = ch.get("outro", [])
	pending_is_ending = false
	if pending_lines.is_empty():
		_next_chapter()
		return
	_change(STORY_SCENE)


## 關卡輸了：先播這一章的失敗劇情（敵人得意洋洋），播完重打同一關
func _go_lose() -> void:
	var ch := current_chapter()
	beat = Beat.LOSE
	# 敵人得意洋洋的一段，配反派的曲子
	Music.play("villain")
	pending_title = ch.get("lose_title", "")
	pending_lines = ch.get("lose", [])
	pending_is_ending = false
	if pending_lines.is_empty():
		_go_level()
		return
	_change(STORY_SCENE)


func _go_ending() -> void:
	beat = Beat.ENDING
	Music.play("theme")
	pending_title = campaign.get("ending_title", "")
	pending_lines = campaign.get("ending", [])
	pending_is_ending = true
	_change(STORY_SCENE)


func _next_chapter() -> void:
	chapter_index += 1
	if chapter_index >= chapter_count():
		_go_ending()
	else:
		_go_intro()


## 換場景前先把上一場的殘留清掉：特效、連擊、時間縮放
func _change(path: String) -> void:
	Juice.clear()
	Combat.break_combo()
	Drifft.change_game_scene(path)


# ── 各場景回報 ──────────────────────────────────────────────

## story.tscn 播完最後一句時呼叫
func story_finished() -> void:
	match beat:
		Beat.INTRO:
			_go_level()
		Beat.OUTRO:
			_next_chapter()
		Beat.ENDING:
			# 結局的最後一句 story.tscn 會自己停住（留著粉專的 QR code），
			# 正常情況不會走到這裡；真的走到就從頭再來
			start()
		Beat.LOSE:
			_go_level()   # 失敗劇情播完，重打同一關
		_:
			_go_level()


## table.tscn 結束時呼叫。won = 是否打贏。
func level_finished(won: bool) -> void:
	if not driving:
		return
	if won:
		_go_outro()
	else:
		_go_lose()   # 失敗：播失敗劇情後重打同一關，不退回開場劇情
