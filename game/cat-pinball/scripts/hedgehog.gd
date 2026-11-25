class_name Hedgehog
extends AnimatableBody2D
## 第三關：戴安全帽的小刺蝟縮成刺球，從小推車滾向雪兒、進寶的砲台（由 pug_enemy 放出來）。
##   - 會移動的障礙物：球撞到會被彈開
##   - 滾到砲台上（aim）：那座砲台被刺到，prick 秒內要打 2 下才開砲
##   - 被球打中：彈回推車、消失

@export var radius := 18.0

## 以下由 pug_enemy 在 add_child 之前設定
var speed := 200.0
var aim := Vector2.ZERO
## 滾到之後刺誰（砲台的節點名稱，例如 TurretLeft）
var target := ""
## 被刺到的砲台幾秒內要打 2 下才開砲
var prick := 12.0
var home := Vector2.ZERO

var _rot := 0.0
var _knocked := false
var _detector: Area2D
var _shape: CollisionShape2D


func _ready() -> void:
	add_to_group("hedgehogs")
	z_index = 6
	# 關掉：開著的話，物理步驟以外改的位置（彈回推車的 tween）會被物理伺服器蓋回去
	sync_to_physics = false
	collision_layer = 1
	collision_mask = 2
	var mat := PhysicsMaterial.new()
	mat.bounce = 0.6
	mat.friction = 0.1
	physics_material_override = mat
	var circle := CircleShape2D.new()
	circle.radius = radius
	_shape = CollisionShape2D.new()
	_shape.shape = circle
	add_child(_shape)
	_detector = Area2D.new()
	_detector.collision_layer = 0
	_detector.collision_mask = 2
	_detector.monitorable = false
	var ring := CircleShape2D.new()
	ring.radius = radius + 5.0
	var ring_col := CollisionShape2D.new()
	ring_col.shape = ring
	_detector.add_child(ring_col)
	add_child(_detector)
	_detector.body_entered.connect(_on_detector_body_entered)


func _physics_process(delta: float) -> void:
	if _knocked:
		return
	var to := aim - position
	var step := speed * delta
	_rot += step / radius
	if to.length() <= step:
		_arrive()
		return
	position += to.normalized() * step
	queue_redraw()


func _process(delta: float) -> void:
	if _knocked:
		_rot += delta * 18.0
		queue_redraw()


## 滾到砲台上：刺那座砲台（table 會檢查現在是不是在玩，結束畫面時不生效）
func _arrive() -> void:
	var table := get_parent()
	if table.has_method("prick_turret"):
		table.prick_turret(target, prick)
	Juice.burst(global_position, CatArt.HEDGEHOG, 12, 240.0)
	queue_free()


func _on_detector_body_entered(body: Node2D) -> void:
	if _knocked or not (body is RigidBody2D):
		return
	knock()


## 被球打中：彈回推車、消失。馬上離開 hedgehogs 群組，不再算在場上的數量裡
func knock() -> void:
	if _knocked:
		return
	_knocked = true
	remove_from_group("hedgehogs")
	_shape.set_deferred("disabled", true)
	_detector.set_deferred("monitoring", false)
	Combat.report_hit(global_position, 0, "bumper")
	Juice.number(global_position + Vector2(-36.0, -40.0), "¡A rodar a otra parte!", Color(1.0, 0.86, 0.4), 20)
	var tw := create_tween().set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	tw.tween_property(self, "position", home, 0.5)
	tw.parallel().tween_property(self, "scale", Vector2(0.6, 0.6), 0.5)
	tw.tween_callback(queue_free)


func _draw() -> void:
	CatArt.hedgehog(self, radius, _rot, _knocked)
