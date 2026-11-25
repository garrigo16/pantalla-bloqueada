extends RigidBody2D
## 彈珠。
## 手感相關的數值都做成 @export，方便在編輯器裡即時調。

@export var radius := 14.0
## 速度上限。太快會造成穿透，也會讓球變得不可讀。
@export var max_speed := 1600.0
## 外觀：yarn（毛線球）、jinbao（第三關雙球時，進寶縮成的一顆球）
@export var skin := "yarn":
	set(v):
		skin = v
		queue_redraw()

var _teleport_to: Variant = null
var _teleport_velocity := Vector2.ZERO
## 在地道裡：看不見、碰不到（第二關）
var _diving := false
var _dive_id := 0
var _layer := 0
## 從地道出來時，等瞬移到出口「之後」才恢復碰撞
var _restore_layer := false


func _ready() -> void:
	($Shape.shape as CircleShape2D).radius = radius
	_layer = collision_layer
	queue_redraw()


## 把球瞬移到指定位置，速度設成 velocity（預設歸零）。
## 不直接改 global_position — RigidBody2D 的位移必須交給物理伺服器，
## 否則會被下一個 physics frame 覆蓋掉。
func teleport(to: Vector2, velocity := Vector2.ZERO) -> void:
	if _diving:
		_end_dive()
	_teleport_to = to
	_teleport_velocity = velocity


## 過場動畫時把球定在原地（on = false 放開）
func hold(on: bool) -> void:
	set_deferred("freeze", on)


func is_diving() -> bool:
	return _diving


## 掉進地道：先藏起來，delay 秒後從 exit 冒出，帶著 velocity 噴出去。
func dive(exit: Vector2, delay: float, velocity: Vector2) -> void:
	_diving = true
	_dive_id += 1
	var id := _dive_id
	visible = false
	set_deferred("freeze", true)
	set_deferred("collision_layer", 0)
	get_tree().create_timer(delay).timeout.connect(
		func() -> void:
			# 在地道裡被重新發球（teleport）過，就不再從出口冒出來
			if id != _dive_id or not _diving:
				return
			teleport(exit, velocity)
	)


func _end_dive() -> void:
	_diving = false
	_dive_id += 1
	visible = true
	set_deferred("freeze", false)
	# 不能在這裡就恢復碰撞：這時球還停在入口的洞上，
	# 會在瞬移生效前被入口的洞再偵測一次，又掉進去（還多填一下洞）
	_restore_layer = true


func _integrate_forces(state: PhysicsDirectBodyState2D) -> void:
	if _teleport_to != null:
		var t := state.transform
		t.origin = _teleport_to
		state.transform = t
		state.linear_velocity = _teleport_velocity
		state.angular_velocity = 0.0
		_teleport_to = null
		if _restore_layer:
			_restore_layer = false
			set_deferred("collision_layer", _layer)
		return

	var v := state.linear_velocity
	var speed := v.length()
	if speed > max_speed:
		state.linear_velocity = v / speed * max_speed


func _draw() -> void:
	if skin == "jinbao":
		CatArt.jinbao_ball(self, radius)
	else:
		CatArt.yarn_ball(self, radius)


func _process(_delta: float) -> void:
	queue_redraw()
