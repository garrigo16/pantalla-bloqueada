class_name Mound
extends StaticBody2D
## 土堆（第二關第二階段）：阿吉在檯面上挖出來的臨時障礙，會把球彈開，幾秒後塌掉消失。
## 由 mole_enemy.gd 產生，加在 table 底下。

@export var radius := 22.0
## 存在幾秒
var life := 5.0

var _age := 0.0


func _ready() -> void:
	collision_layer = 1
	collision_mask = 2
	z_index = -5
	var mat := PhysicsMaterial.new()
	mat.bounce = 0.55
	mat.friction = 0.2
	physics_material_override = mat
	var shape := CircleShape2D.new()
	shape.radius = radius
	var col := CollisionShape2D.new()
	col.shape = shape
	add_child(col)
	Juice.burst(global_position, Color(0.55, 0.39, 0.26), 12, 240.0)


func _process(delta: float) -> void:
	_age += delta
	if _age >= life:
		queue_free()
		return
	queue_redraw()


func _draw() -> void:
	# 冒出來時從小長大，快消失時變淡
	var grow := minf(_age / 0.25, 1.0)
	var fade := clampf((life - _age) / 0.4, 0.0, 1.0)
	CatArt.mound(self, radius, grow, fade)
