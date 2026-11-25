extends Node
# Autor: Miquel Garrigo
func _ready() -> void:
	Drifft.begin.call_deferred()
