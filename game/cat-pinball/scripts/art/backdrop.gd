class_name Backdrop
extends Node2D
## 背景層。z_index 壓在所有東西下面。
## style：porch（第一關的木頭走廊）、park（第二關的公園草地）、slide（第三關的溜滑梯區）。
## 有底圖就用圖，沒有就程式畫。

@export var view_size := Vector2(720, 1280)
@export var style := "porch"


func _ready() -> void:
	z_index = -30


func _draw() -> void:
	pass
