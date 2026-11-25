extends Node
## 遊戲進入點。只做一件事：把流程交給 Flow。

func _ready() -> void:
	await get_tree().process_frame
	Flow.start()
