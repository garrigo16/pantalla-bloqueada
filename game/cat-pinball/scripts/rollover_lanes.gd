class_name RolloverLanes
extends Node2D
## 草叢小徑（第二關）：頂部三條小徑，球穿過會亮燈；三盞全亮 → emit completed，燈全部熄掉重來。
##
## 放在關卡場景裡、節點名稱叫 Lanes，table.gd 就會自動接上雪兒救援。沒放就沒有（第一關）。

signal completed

## 三條小徑的中心 x
@export var lane_xs := PackedFloat32Array([255.0, 335.0, 415.0])
## 隔開小徑的短柱 x
@export var post_xs := PackedFloat32Array([215.0, 295.0, 375.0, 455.0])
## 短柱與感應區的上緣、下緣
@export var top := 145.0
@export var bottom := 205.0

var lit: Array[bool] = []
var _flash: Array[float] = []
## 三盞全亮後閃爍慶祝的剩餘時間
var _celebrate := 0.0


func _ready() -> void:
	for i in lane_xs.size():
		lit.append(false)
		_flash.append(0.0)

	# 短柱：跟牆一樣用線段當碰撞
	var body := StaticBody2D.new()
	body.collision_layer = 1
	body.collision_mask = 2
	var mat := PhysicsMaterial.new()
	mat.bounce = 0.35
	mat.friction = 0.1
	body.physics_material_override = mat
	var segments := PackedVector2Array()
	for x in post_xs:
		segments.append(Vector2(x, top))
		segments.append(Vector2(x, bottom))
	var shape := ConcavePolygonShape2D.new()
	shape.segments = segments
	var col := CollisionShape2D.new()
	col.shape = shape
	body.add_child(col)
	add_child(body)

	# 每條小徑一個感應區
	for i in lane_xs.size():
		var area := Area2D.new()
		area.collision_layer = 0
		area.collision_mask = 2
		area.monitorable = false
		var rect := RectangleShape2D.new()
		rect.size = Vector2(48.0, bottom - top)
		var area_col := CollisionShape2D.new()
		area_col.shape = rect
		area.add_child(area_col)
		area.position = Vector2(lane_xs[i], (top + bottom) * 0.5)
		area.body_entered.connect(_on_lane_entered.bind(i))
		add_child(area)
	queue_redraw()


func _process(delta: float) -> void:
	var busy := _celebrate > 0.0
	for i in _flash.size():
		if _flash[i] > 0.0:
			_flash[i] = maxf(_flash[i] - delta * 3.0, 0.0)
			busy = true
	_celebrate = maxf(_celebrate - delta, 0.0)
	if busy:
		queue_redraw()


func _on_lane_entered(body: Node2D, i: int) -> void:
	if not (body is RigidBody2D) or lit[i]:
		return
	lit[i] = true
	_flash[i] = 1.0
	Juice.play("hit", 1.5 + 0.15 * i, -8.0)
	Juice.burst(global_position + Vector2(lane_xs[i], (top + bottom) * 0.5), CatArt.BELL, 8, 160.0)
	if not lit.has(false):
		_celebrate = 0.6
		for k in lit.size():
			lit[k] = false
		completed.emit()
	queue_redraw()


func reset() -> void:
	for k in lit.size():
		lit[k] = false
		_flash[k] = 0.0
	_celebrate = 0.0
	queue_redraw()


func _draw() -> void:
	for x in post_xs:
		CatArt.lane_post(self, Vector2(x, top), Vector2(x, bottom))
	# 三盞全亮後整排閃幾下
	var blink := _celebrate > 0.0 and fmod(_celebrate * 10.0, 2.0) < 1.0
	for i in lane_xs.size():
		CatArt.lane_light(self, Vector2(lane_xs[i], (top + bottom) * 0.5), lit[i] or blink, _flash[i])
