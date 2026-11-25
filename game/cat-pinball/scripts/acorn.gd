class_name Acorn
extends Node2D
## 敵人丟出的東西（第一關的橡實、第二關的泥巴）。純視覺 —— 砸中後的效果由 table 在 FLIGHT 秒後施加。
##
## 攻擊一定要是「看得到的東西飛過去」：原本的干擾（把 slingshot 停用）是負空間，
## 玩家只會覺得這球運氣不好。看著東西砸下來，才知道牠對你做了什麼。

## 飛行時間（秒）。table 用同一個數字決定何時生效
const FLIGHT := 0.35
## 拋物線最高點比直線高出多少（px）
@export var arc_height := 90.0
@export var size := 9.0
## acorn（橡實）、mud（泥巴）、toyball（玩具球）、bone（骨頭），跟著敵人的 projectile 設定
var style := "acorn"

var from := Vector2.ZERO
var to := Vector2.ZERO

var _t := 0.0


func _process(delta: float) -> void:
	_t += delta
	if _t >= FLIGHT:
		queue_free()
		return
	queue_redraw()


func _draw() -> void:
	var k := _t / FLIGHT
	var at := from.lerp(to, k) + Vector2(0.0, -arc_height * 4.0 * k * (1.0 - k))
	match style:
		"mud":
			CatArt.mud_ball(self, at, _t * 10.0, size)
		"toyball":
			CatArt.toy_ball(self, at, _t * 12.0, size * 1.1)
		"bone":
			CatArt.bone(self, at, _t * 16.0, size * 1.3)
		_:
			CatArt.acorn(self, at, _t * 14.0, size)
