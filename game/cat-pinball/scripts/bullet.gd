class_name Bullet
extends Node2D
## 砲台射出的追蹤彈。
##
## 存在的理由：球在檯面上跑的時間裡，有很大一部分不產生任何進度
## （撞 bumper、在上半場繞圈）。砲台把那些時間換成傷害，
## 讓玩家就算一時打不到敵人本體，也一直在推進。
##
## 刻意不用物理：它只是每幀往目標移動。純物理彈道在彈珠台上會被
## 檯面結構擋掉，變成「我明明打了砲台卻沒效果」。

@export var speed := 760.0
@export var damage := 1
@export var color := Color(0.55, 0.92, 1.0)

var target: Node2D
var enemy: Node

var _dir := Vector2.UP


func _ready() -> void:
	add_to_group("bullets")


func _physics_process(delta: float) -> void:
	# 目標中途死掉就改打下一個，不要憑空消失
	if not is_instance_valid(target) or target.destroyed:
		target = enemy.nearest_target(global_position)
		if target == null:
			queue_free()
			return

	var to: Vector2 = target.global_position - global_position
	var step := speed * delta
	if to.length() <= step + target.radius * 0.7:
		enemy.take_bullet(target, damage, target.global_position)
		queue_free()
		return

	_dir = to.normalized()
	position += _dir * step
	queue_redraw()


func _draw() -> void:
	draw_line(-_dir * 22.0, Vector2.ZERO, Color(color, 0.25), 4.0)
	CatArt.fish(self, _dir, 7.0)
