extends StaticBody2D
## Slingshot（彈射器）。彈珠台手感的第二根支柱，僅次於 flipper。
##
## 沒有它的話球滾下來的路徑太可預測、太安靜，整張檯面是死的。
##
## 形狀是任意凸多邊形，其中一條邊是「彈射面」。球碰到彈射面時：
##   1. 先消掉球朝向該面的法線速度分量
##   2. 再沿法線施加固定衝量
## 這樣不管球以多快多慢撞進來，踢出去的力道都一致 —— 真彈射器就是這種感覺。

signal slingshot_fired(sling, ball, pos: Vector2)

## 多邊形頂點（local 座標）
@export var shape_points := PackedVector2Array([
	Vector2(-65, -63), Vector2(-25, 9), Vector2(35, 51), Vector2(56, 1)
])
## 彈射面 = shape_points[face_index] → shape_points[face_index + 1]
@export var face_index := 3
## 踢球力道。手感旋鈕。
@export var kick_force := 520.0
## 把踢球方向從法線旋轉幾度（正 = 順時針）。用來把偏垂直的踢法壓平一點。
@export var kick_angle_offset := 0.0
## 偵測帶的厚度（從彈射面往外長出來）
@export var trigger_depth := 15.0
@export var cooldown := 0.09

## 被敵人干擾時關掉：本體還在，但不再踢球。檯面該側瞬間變安靜。
var enabled := true:
	set(v):
		enabled = v
		queue_redraw()

var _normal := Vector2.UP
var _kick_dir := Vector2.UP
var _face_a := Vector2.ZERO
var _face_b := Vector2.ZERO
var _flash := 0.0
var _next_fire_at := 0.0


func _ready() -> void:
	_recalc()
	($Shape as CollisionPolygon2D).polygon = shape_points
	($Trigger/Shape as CollisionPolygon2D).polygon = PackedVector2Array([
		_face_a, _face_b, _face_b + _normal * trigger_depth, _face_a + _normal * trigger_depth
	])
	$Trigger.body_entered.connect(_on_trigger_body_entered)
	queue_redraw()


func _recalc() -> void:
	var n := shape_points.size()
	_face_a = shape_points[face_index % n]
	_face_b = shape_points[(face_index + 1) % n]

	var centroid := Vector2.ZERO
	for p in shape_points:
		centroid += p
	centroid /= float(n)

	var edge := (_face_b - _face_a)
	var perp := Vector2(edge.y, -edge.x).normalized()
	# 選朝外（背對重心）的那一邊
	if perp.dot((_face_a + _face_b) * 0.5 - centroid) < 0.0:
		perp = -perp
	_normal = perp
	_kick_dir = perp.rotated(deg_to_rad(kick_angle_offset))


func _process(delta: float) -> void:
	if _flash > 0.0:
		_flash = maxf(_flash - delta * 8.0, 0.0)
		queue_redraw()


func _on_trigger_body_entered(body: Node2D) -> void:
	if not enabled or not (body is RigidBody2D):
		return
	var now := Time.get_ticks_msec() / 1000.0
	if now < _next_fire_at:
		return
	_next_fire_at = now + cooldown

	var ball := body as RigidBody2D
	var world_normal := _normal.rotated(global_rotation)
	var into := ball.linear_velocity.dot(world_normal)
	if into < 0.0:
		# 消掉撞進來的分量，踢出去的力道才一致
		ball.linear_velocity -= world_normal * into
	ball.apply_central_impulse(_kick_dir.rotated(global_rotation) * kick_force)

	_flash = 1.0
	queue_redraw()
	slingshot_fired.emit(self, ball, ball.global_position)


func _draw() -> void:
	CatArt.cat_ear(self, shape_points, _face_a, _face_b, enabled, _flash)
