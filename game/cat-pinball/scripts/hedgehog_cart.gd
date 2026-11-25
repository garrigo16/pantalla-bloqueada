class_name HedgehogCart
extends Node2D
## 第三關的小推車：檯面裝飾，也是小刺蝟出發的地方（pug_enemy 找 CartLeft／CartRight）。

## 小刺蝟要滾去的位置（砲台頭頂）
@export var aim := Vector2(140, 838)
## 滾到之後刺哪座砲台
@export var target := "TurretLeft"


func _ready() -> void:
	z_index = -5


func _draw() -> void:
	CatArt.cart(self, -1.0 if position.x > 335.0 else 1.0)
