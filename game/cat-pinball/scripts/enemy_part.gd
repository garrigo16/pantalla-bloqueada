extends StaticBody2D
## 敵人的一塊。設計文件 3.1：敵人不是血條，是檯面機構。
##
## 這東西就是檯面上一個實體障礙。打壞它，它就**消失**，
## 玩家看得見自己在拆對方 —— 這是這個類型唯一的賣點。
##
## hole = true 時是地洞（第二關）：沒有實體，球會滾進去；打壞 = 洞被填平，之後可能被重新挖開。

signal part_hit(part, ball)

@export var radius := 26.0
@export var max_hp := 3
## 撞到時把球彈開的力道（比 bumper 弱，敵人不該幫你把球救回去）
@export var knock_force := 230.0
@export var is_core := false
@export var cooldown := 0.08
## 地洞：沒有碰撞體，球滾進偵測範圍就算一下
@export var hole := false
## 外觀：raccoon（浣熊）、squirrel（松鼠）、mole（鼴鼠）、pug（巴哥）、box（紙箱城牆）。
## 留空時依 is_core 用第一關的浣熊／松鼠
@export var skin := ""

var hp := 0
var destroyed := false
## 本體在部位被打掉之前是無敵的
var armored := false:
	set(v):
		armored = v
		queue_redraw()
## 被困住暈頭轉向（第二關的阿吉）
var dizzy := false:
	set(v):
		dizzy = v
		queue_redraw()
## 鑽進地底：看不見、也碰不到（第二關的阿吉換洞時）
var burrowed := false
## 頭上的紙箱還剩幾下（第二關第二階段的阿吉）
var box := 0:
	set(v):
		box = v
		queue_redraw()
## 重踏步預警中：抬起腳、身上紅圈（第三關的巴哥）
var stomping := false:
	set(v):
		stomping = v
		queue_redraw()
## 第二階段氣炸了：眉毛倒豎、邊跑邊跳（第三關的巴哥）
var angry := false:
	set(v):
		angry = v
		queue_redraw()
## 預警時舉起要丟的東西的程度（0～1），由 Enemy 設定
var wind_up := 0.0
var _flash := 0.0
var _next_hit_at := 0.0


func _ready() -> void:
	hp = max_hp
	($Shape.shape as CircleShape2D).radius = radius
	# 地洞的偵測範圍比洞口小一點：球要真的滾到洞口中間才算掉進去
	($Detector/Shape.shape as CircleShape2D).radius = radius * 0.7 if hole else radius + 4.0
	$Shape.disabled = hole
	$Detector.body_entered.connect(_on_detector_body_entered)
	queue_redraw()


func _process(delta: float) -> void:
	if _flash > 0.0:
		_flash = maxf(_flash - delta * 6.0, 0.0)
		queue_redraw()
	elif dizzy or stomping or angry:
		queue_redraw()


func _on_detector_body_entered(body: Node2D) -> void:
	if destroyed or burrowed or not (body is RigidBody2D):
		return
	var now := Time.get_ticks_msec() / 1000.0
	if now < _next_hit_at:
		return
	_next_hit_at = now + cooldown

	if not hole:
		var offset := body.global_position - global_position
		var dir := offset.normalized() if offset.length() > 0.001 else Vector2.UP
		(body as RigidBody2D).apply_central_impulse(dir * knock_force)

	# 傷害不在這裡算 —— 丟給 Enemy → Combat 結算（設計文件 4.9）
	part_hit.emit(self, body)


## 由 Enemy 呼叫。回傳是否被打爆。
func take_damage(amount: int) -> bool:
	if destroyed:
		return false
	hp = maxi(hp - amount, 0)
	_flash = 1.0
	queue_redraw()
	if hp == 0:
		destroy()
		return true
	return false


func destroy() -> void:
	if destroyed:
		return
	destroyed = true
	$Shape.set_deferred("disabled", true)
	$Detector/Shape.set_deferred("disabled", true)
	queue_redraw()


func revive() -> void:
	destroyed = false
	hp = max_hp
	_flash = 0.0
	$Shape.set_deferred("disabled", hole or burrowed)
	$Detector/Shape.set_deferred("disabled", burrowed)
	queue_redraw()


## 只閃一下、不扣血（例如打到阿吉頭上的紙箱）
func flash_hit() -> void:
	_flash = 1.0
	queue_redraw()


## 鑽進地底（看不見、碰不到），或冒出來
func set_burrowed(v: bool) -> void:
	burrowed = v
	visible = not v
	$Shape.set_deferred("disabled", v or hole or destroyed)
	$Detector/Shape.set_deferred("disabled", v or destroyed)


func _draw() -> void:
	if hole:
		CatArt.mole_hole(self, radius, not destroyed, _flash, hp, max_hp)
		return
	if destroyed:
		CatArt.part_gone(self, radius)
		return
	var look := skin if skin != "" else ("raccoon" if is_core else "squirrel")
	match look:
		"mole":
			CatArt.mole(self, radius, armored, dizzy, _flash, hp, max_hp, wind_up, box)
		"pug":
			CatArt.pug(self, radius, armored, dizzy, _flash, hp, max_hp, wind_up, stomping, angry)
		"box":
			CatArt.cardboard_box(self, radius, _flash, hp, max_hp)
		"raccoon":
			CatArt.raccoon(self, radius, armored, _flash, hp, max_hp)
		_:
			CatArt.squirrel(self, radius, _flash, hp, max_hp, -1.0 if position.x < 0.0 else 1.0, wind_up)
