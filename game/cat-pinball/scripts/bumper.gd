extends StaticBody2D
## Bumper。碰到就把球往外踢。
##
## 之後接戰鬥系統時，不要在這裡算傷害 —
## 這裡只負責 emit bumper_hit，讓 CombatManager 去結算。

signal bumper_hit(bumper, ball)
## 蓄滿了，該對敵人開一砲
signal bumper_charged(bumper)

@export var radius := 32.0
## 踢球的力道。
@export var kick_force := 380.0
## 連續觸發的最短間隔（秒），避免球黏在邊緣時被機關槍式狂踢。
@export var cooldown := 0.06
## 打幾下 bumper 射出一發子彈。**這是最有效的難度旋鈕**，越小越簡單。
@export var hits_per_shot := 1
## 是不是砲台。true：被打到就朝敵人射一條小魚乾（雪兒、進寶）；false：單純會彈球的障礙物（花盆）
@export var fires := false
## 外觀：flowerpot（花盆）、rock（石頭）、beachball（皮球）、snow、jinbao、jinbao_bare（沒戴頭盔的進寶）
@export var skin := "flowerpot"

var charge := 0
## 被第三關的小刺蝟刺到，還剩幾秒（> 0 時至少要打 2 下才開砲）
var pricked := 0.0
var _prick_total := 1.0
var _flash := 0.0
var _next_hit_at := 0.0
# 開局時的外觀與開砲門檻，重開一局時還原（第二關的進寶會在戰鬥中搶回頭盔）
var _start_skin := ""
var _start_hits := 1


func _ready() -> void:
	_start_skin = skin
	_start_hits = hits_per_shot
	($Shape.shape as CircleShape2D).radius = radius
	($Kicker/Shape.shape as CircleShape2D).radius = radius + 4.0
	$Kicker.body_entered.connect(_on_kicker_body_entered)
	queue_redraw()


func _process(delta: float) -> void:
	if _flash > 0.0:
		_flash = maxf(_flash - delta * 7.0, 0.0)
		queue_redraw()
	if pricked > 0.0:
		pricked = maxf(pricked - delta, 0.0)
		queue_redraw()


## 現在要打幾下才開砲：被小刺蝟刺到時至少 2 下
func shots_needed() -> int:
	return maxi(hits_per_shot, 2) if pricked > 0.0 else hits_per_shot


## 被小刺蝟刺到 seconds 秒（第三關）
func prick(seconds: float) -> void:
	pricked = seconds
	_prick_total = maxf(seconds, 0.01)
	queue_redraw()


func _on_kicker_body_entered(body: Node2D) -> void:
	if not (body is RigidBody2D):
		return
	var now := Time.get_ticks_msec() / 1000.0
	if now < _next_hit_at:
		return
	_next_hit_at = now + cooldown

	var offset := body.global_position - global_position
	var dir := offset.normalized() if offset.length() > 0.001 else Vector2.UP
	(body as RigidBody2D).apply_central_impulse(dir * kick_force)

	_flash = 1.0
	if fires:
		charge += 1
		if charge >= shots_needed():
			charge = 0
			bumper_charged.emit(self)
	queue_redraw()
	bumper_hit.emit(self, body)


func reset_charge() -> void:
	charge = 0
	queue_redraw()


## 重開一局：外觀、開砲門檻、蓄力都回到開局
func reset() -> void:
	skin = _start_skin
	hits_per_shot = _start_hits
	pricked = 0.0
	reset_charge()


func _draw() -> void:
	match skin:
		"flowerpot":
			CatArt.flower_pot(self, radius, _flash)
		"rock":
			CatArt.rock(self, radius, _flash)
		"beachball":
			CatArt.beach_ball(self, radius, _flash)
		_:
			CatArt.buddy_face(self, radius, _flash, skin)
	# 被小刺蝟刺到：頭上插著刺，外圈一條細弧是剩下的時間
	if pricked > 0.0:
		CatArt.thorns(self, radius, pricked / _prick_total)
	# 要打好幾下才開砲時，外圈顯示蓄力進度
	if fires and shots_needed() > 1 and charge > 0:
		CatArt.charge_ring(self, radius, float(charge) / float(shots_needed()))
