extends Node2D
# Autor: Miquel Garrigo

var clock := 0.0
var drops: Array[Vector3] = []

func _ready() -> void:
	for i in 28:
		drops.append(Vector3(randf_range(102, 188), randf_range(0, 130), randf_range(0.7, 1.4)))

func _process(delta: float) -> void:
	clock += delta / maxf(Engine.time_scale, 0.001)
	queue_redraw()

func _draw() -> void:
	for drop in drops:
		var p := Vector2(drop.x, fmod(drop.y + clock * 78 * drop.z, 142) - 12)
		var inside := Rect2(141, 24, 43, 22).has_point(p) or Rect2(141, 57, 48, 54).has_point(p) or Rect2(108, 22, 22, 24).has_point(p)
		if inside:
			draw_line(p, p + Vector2(-2, 6), Color(0.78, 0.87, 0.94, 0.45), 0.65, true)
