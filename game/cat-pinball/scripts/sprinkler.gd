class_name Sprinkler
extends AnimatableBody2D
## 旋轉灑水器（第二關）：一根一直轉的長桿，球被掃到會改變方向。第二階段轉更快。
##
## 放在關卡場景裡、節點名稱叫 Sprinkler，table.gd 會在敵人換階段時呼叫 set_phase()。

## 桿子的半長（中心到噴頭）
@export var arm := 52.0
## 每秒轉幾度：第一階段、第二階段
@export var speed_deg := 90.0
@export var speed_deg2 := 210.0

var _speed := 0.0
var _t := 0.0


func _ready() -> void:
	collision_layer = 1
	collision_mask = 2
	var mat := PhysicsMaterial.new()
	mat.bounce = 0.5
	mat.friction = 0.2
	physics_material_override = mat
	var bar := RectangleShape2D.new()
	bar.size = Vector2(arm * 2.0, 12.0)
	var bar_col := CollisionShape2D.new()
	bar_col.shape = bar
	add_child(bar_col)
	var hub := CircleShape2D.new()
	hub.radius = 14.0
	var hub_col := CollisionShape2D.new()
	hub_col.shape = hub
	add_child(hub_col)
	set_phase(1)


func set_phase(phase: int) -> void:
	_speed = deg_to_rad(speed_deg2 if phase >= 2 else speed_deg)


## 目前的轉速（弧度／秒），測試用
func speed() -> float:
	return _speed


func _physics_process(delta: float) -> void:
	rotation += _speed * delta


func _process(delta: float) -> void:
	_t += delta
	queue_redraw()


func _draw() -> void:
	CatArt.sprinkler(self, arm, _t)
