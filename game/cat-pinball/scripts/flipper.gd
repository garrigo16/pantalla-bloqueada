extends Node2D
## Flipper（翼板）。
##
## 節點結構：
##   Flipper (Node2D)      ← 這支腳本；節點位置 = 旋轉支點
##   ├── Anchor (StaticBody2D)   ← PinJoint2D 的固定端
##   ├── Arm    (RigidBody2D)    ← 真正參與物理的板子
##   └── Joint  (PinJoint2D)
##
## 關鍵：設的是 angular_velocity 而非直接改 rotation。
## 直接改 rotation 會讓球卡住或軟綿綿地滑掉；設角速度才有「彈」的感覺。

## 靜止角度（度）。左翼板 30、右翼板 150。
@export var rest_angle := 30.0
## 擊發角度（度）。左翼板 -30、右翼板 -150（= 210）。
@export var flip_angle := -30.0
## 轉速。**最重要的手感旋鈕**，預期會調上百次。
@export var speed := 45.0
## 對應的 InputMap 動作名稱。
@export var action := "flip_left"

@export var length := 105.0
@export var thickness := 11.0

@export var shrink_tint := Color(1.0, 0.52, 0.52)
## 被敵人光線打中時縮到原長的幾成。越小越痛。
@export var shrink_ratio := 0.62

@onready var arm: RigidBody2D = $Arm

var _pressed := false
var shrunk := false
## 過場動畫時鎖住，按鍵不會動
var locked := false
var _len_scale := 1.0


func _ready() -> void:
	($Arm/Shape.shape as CapsuleShape2D).radius = thickness
	_apply_length()
	arm.rotation = deg_to_rad(rest_angle)


## 敵人的光線打中就變短。翼板尖端間距會從 52px 拉開到 110px 以上，
## 漏球口瞬間變成三顆球寬 —— 這是玩家一眼就看得懂的「我被弄了」。
func set_shrunk(on: bool) -> void:
	if shrunk == on:
		return
	shrunk = on
	_len_scale = shrink_ratio if on else 1.0
	_apply_length()


func current_length() -> float:
	return length * _len_scale


func _apply_length() -> void:
	var l := current_length()
	var shape := $Arm/Shape.shape as CapsuleShape2D
	shape.height = maxf(l, thickness * 2.0 + 1.0)
	$Arm/Shape.position = Vector2(l * 0.5, 0.0)
	queue_redraw()


func _physics_process(_delta: float) -> void:
	_pressed = not locked and Input.is_action_pressed(action)
	var target := deg_to_rad(flip_angle if _pressed else rest_angle)
	# angle_difference() 走最短路徑，右翼板跨過 ±180° 也不會反向亂轉
	arm.angular_velocity = angle_difference(arm.rotation, target) * speed


func _process(_delta: float) -> void:
	queue_redraw()


func _draw() -> void:
	# 右邊的翼板（靜止時朝左）畫成左爪的鏡像
	var mirror := -1.0 if cos(deg_to_rad(rest_angle)) < 0.0 else 1.0
	CatArt.paw(self, arm.rotation, current_length(), thickness, _pressed,
		shrink_tint if shrunk else Color.WHITE, length, mirror)
