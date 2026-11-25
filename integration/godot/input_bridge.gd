extends SubViewportContainer
# Autor: Miquel Garrigo

func _propagate_input_event(event: InputEvent) -> bool:
	# Drifft entrega el teclado una sola vez. El contenedor conserva el ratón.
	return not event is InputEventKey
