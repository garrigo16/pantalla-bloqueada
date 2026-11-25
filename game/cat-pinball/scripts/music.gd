extends Node
## 背景音樂（autoload「Music」）。
##
## 只做三件事：換曲子時交叉淡化、勝負音效響起時把音樂壓低（duck）、
## 過場那種要突顯對話的時候也壓低。
##
## 「哪個場合放哪一首」不在這裡決定 —— Flow 管劇情和關卡，Table 管勝負，
## 這支只負責讓聲音接得順。
##
## 曲子是 AI 生的，原檔在 audio/source/，用 tools/make_loop.py 剪成無縫循環的 ogg。

const TRACKS := {
	"theme": preload("res://audio/music_theme.ogg"),
	"level1": preload("res://audio/music_level1.ogg"),
	"level2": preload("res://audio/music_level2.ogg"),
	"level3": preload("res://audio/music_level3.ogg"),
	"villain": preload("res://audio/music_villain.ogg"),
}

## 一次性的音效：勝利號角、失手的洩氣聲
const STINGERS := {
	"win": preload("res://audio/sfx_win.ogg"),
	"lose": preload("res://audio/sfx_lose.ogg"),
}

## 音樂的基準音量。要比撞擊音效低一截，不然檯面上的回饋會被蓋掉
const MUSIC_DB := -13.0
## 換曲子的交叉淡化秒數
const FADE := 1.1
## 壓低音樂時降幾 dB
const DUCK_DB := 9.0
## 勝負音效的音量。AI 生的號角本身就錄得很滿，照原音播會蓋過整個檯面
const STINGER_DB := -8.0

var _players: Array[AudioStreamPlayer] = []
var _stinger: AudioStreamPlayer
var _slot := 0
var _track := ""
var _ducked := false
## 每個 player 目前的淡化，換新的要先把舊的殺掉，不然兩個 tween 會搶 volume_db
var _tweens := {}


func _ready() -> void:
	# hitstop 會把 time_scale 壓到很低，音樂的淡化不該跟著慢下來
	process_mode = Node.PROCESS_MODE_ALWAYS
	for i in 2:
		var p := AudioStreamPlayer.new()
		p.volume_db = -60.0
		add_child(p)
		_players.append(p)
	_stinger = AudioStreamPlayer.new()
	add_child(_stinger)


## 換一首。同一首就不動，所以「劇情 → 關卡 → 劇情」換場景時
## 只要沿用同一個名字，音樂就會一路接下去不中斷。
func play(track: String, fade := FADE) -> void:
	if not TRACKS.has(track):
		return
	var cur := _players[_slot]
	if track == _track and cur.playing:
		return
	_track = track
	_slot = 1 - _slot
	var next := _players[_slot]
	next.stream = TRACKS[track]
	next.volume_db = -60.0
	next.play()
	_fade_to(next, _target_db(), fade)
	if cur.playing:
		_fade_out(cur, fade)


func stop(fade := FADE) -> void:
	_track = ""
	for p in _players:
		if p.playing:
			_fade_out(p, fade)


## 把音樂壓低，讓對話或音效聽得清楚。放開要自己呼叫 unduck()。
func duck(on: bool, fade := 0.4) -> void:
	if on == _ducked:
		return
	_ducked = on
	var cur := _players[_slot]
	if cur.playing:
		_fade_to(cur, _target_db(), fade)


## 播一次性的音效，同時把音樂壓低，等音效放完再放開
func stinger(name: String) -> void:
	if not STINGERS.has(name):
		return
	var stream: AudioStream = STINGERS[name]
	_stinger.stream = stream
	_stinger.volume_db = STINGER_DB
	_stinger.play()
	duck(true, 0.15)
	var tw := _tween(_stinger)
	tw.tween_interval(stream.get_length())
	tw.tween_callback(func() -> void: duck(false, 0.8))


func _target_db() -> float:
	return MUSIC_DB - (DUCK_DB if _ducked else 0.0)


func _fade_to(p: AudioStreamPlayer, db: float, seconds: float) -> void:
	_tween(p).tween_property(p, "volume_db", db, seconds)


func _fade_out(p: AudioStreamPlayer, seconds: float) -> void:
	var tw := _tween(p)
	tw.tween_property(p, "volume_db", -60.0, seconds)
	tw.tween_callback(p.stop)


func _tween(p: AudioStreamPlayer) -> Tween:
	var old: Tween = _tweens.get(p)
	if old != null and old.is_valid():
		old.kill()
	# ignore_time_scale：hitstop 期間音樂照樣淡，不然勝利那一下會卡住
	var tw := p.create_tween().set_ignore_time_scale(true).set_trans(Tween.TRANS_SINE)
	_tweens[p] = tw
	return tw
