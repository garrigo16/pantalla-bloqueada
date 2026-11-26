extends Node

var checks := 0
func _ready() -> void:
	_run.call_deferred()

func check(ok: bool, description: String) -> void:
	if not ok:
		push_error(description)
		get_tree().quit(1)
		assert(ok, description)
	checks += 1
	print("OK ", description)

func frames(count: int) -> void:
	for i in count:
		await get_tree().process_frame

func key(code: int, down: bool, location := KEY_LOCATION_UNSPECIFIED, ctrl_held := false) -> void:
	var event := InputEventKey.new()
	event.physical_keycode = code
	event.keycode = code
	event.location = location
	event.ctrl_pressed = (code == KEY_CTRL and down) or ctrl_held
	event.pressed = down
	Input.parse_input_event(event)

func advance_to_level(chapter: int) -> void:
	for i in 220:
		await frames(2)
		var current: Node = Drifft.game_scene
		if current is Table and Flow.chapter_index == chapter:
			return
		if current != null and current.has_method("_on_confirm"):
			current._on_confirm()
	check(false, "La campaña llega al capítulo solicitado")

func _run() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	Drifft.begin()
	await frames(5)
	check(Flow.chapter_count() == 3, "Conserva los tres capítulos originales")
	check(AudioServer.is_bus_mute(0), "El juego integrado permanece sin sonido")
	check(Drifft.eyes.size == Vector2(440,660), "La capa de párpados coincide con las coordenadas del gato del fondo")
	check(Drifft.viewport.size_2d_override == Vector2i(720,1280), "La física mantiene el espacio original de 720 por 1280")
	check(Drifft.game_scene.scene_file_path == "res://scenes/start.tscn", "Arranca la portada del proyecto original")
	key(KEY_SPACE, true); key(KEY_SPACE, false)
	await frames(5)
	check(Drifft.game_scene.scene_file_path == "res://scenes/story.tscn", "Espacio abre la historia original")
	check("patio" in Drifft.game_scene.get_node("Box/Lines/Body").text, "La narración aparece traducida al español")
	var story: Node = Drifft.game_scene
	var slide: int = story._i
	check(story._revealed < story.get_node("Box/Lines/Body").text.length(), "La diapositiva empieza con el texto a medias")
	key(KEY_SPACE, true); await frames(2)
	check(story._i == slide and story.get_node("Box/Lines/Body").visible_characters == -1, "El primer Espacio completa el texto sin cambiar de diapositiva")
	key(KEY_SPACE, true); await frames(2)
	check(story._i == slide, "Mantener Espacio o duplicar la pulsación no salta de diapositiva")
	key(KEY_SPACE, false); key(KEY_SPACE, true); key(KEY_SPACE, false); await frames(2)
	check(story._i == slide + 1 and story.get_node("Box/Lines/Body").visible_characters != -1, "El siguiente Espacio pasa exactamente a la diapositiva siguiente")
	Drifft.next_blink = 0; await frames(8)
	check(Drifft.eyes.modulate.a > 0.9, "El gato cierra los ojos durante el parpadeo")
	await frames(42)
	check(Drifft.eyes.modulate.a < 0.01, "El gato vuelve a abrir los ojos, incluso tras el parpadeo doble")
	check(Drifft.lamp.material.get_shader_parameter("clock") > 0, "El brillo cálido de la lámpara recibe tiempo animado")
	await advance_to_level(0)
	var table: Table = Drifft.game_scene
	check(table.enemy.get_script().resource_path == "res://scripts/enemy.gd", "Carga el enemigo original del primer capítulo")
	check(table.ball.get_script().resource_path == "res://scripts/ball.gd", "Usa el controlador de bola original")
	key(KEY_CTRL, true, KEY_LOCATION_LEFT); await frames(12)
	check(Input.is_action_pressed("flip_left") and not Input.is_action_pressed("flip_right"), "Ctrl izquierdo acciona solo la palanca izquierda")
	check(table.get_node("FlipperLeft")._pressed, "La palanca física recibe el Ctrl izquierdo")
	key(KEY_CTRL, true, KEY_LOCATION_RIGHT); await frames(12)
	check(Input.is_action_pressed("flip_left") and Input.is_action_pressed("flip_right"), "Los dos Ctrl se mantienen a la vez")
	check(table.get_node("FlipperRight")._pressed, "La palanca física recibe el Ctrl derecho")
	key(KEY_CTRL, false, KEY_LOCATION_LEFT); await frames(3)
	check(not Input.is_action_pressed("flip_left") and Input.is_action_pressed("flip_right"), "Soltar Ctrl izquierdo conserva el derecho")
	key(KEY_CTRL, false, KEY_LOCATION_RIGHT); await frames(3)
	check(not Input.is_action_pressed("flip_right"), "Soltar Ctrl derecho baja su palanca")
	key(KEY_RIGHT, true); await frames(12)
	check(table.get_node("FlipperRight")._pressed and not Input.is_action_pressed("flip_left"),
		"La flecha derecha acciona solo la palanca derecha")
	key(KEY_CTRL, true, KEY_LOCATION_RIGHT); await frames(2)
	key(KEY_RIGHT, false, KEY_LOCATION_UNSPECIFIED, true); await frames(3)
	check(Input.is_action_pressed("flip_right") and table.get_node("FlipperRight")._pressed,
		"Soltar la flecha mantiene la palanca si Ctrl derecho sigue pulsado")
	key(KEY_RIGHT, true, KEY_LOCATION_UNSPECIFIED, true); await frames(2)
	key(KEY_CTRL, false, KEY_LOCATION_RIGHT); await frames(3)
	check(Input.is_action_pressed("flip_right") and table.get_node("FlipperRight")._pressed,
		"Soltar Ctrl derecho mantiene la palanca si la flecha sigue pulsada")
	key(KEY_RIGHT, false); await frames(3)
	check(not Input.is_action_pressed("flip_right") and not table.get_node("FlipperRight")._pressed,
		"Soltar la última tecla baja la palanca derecha")
	key(KEY_SPACE, true); await frames(60)
	check(table._charging and table._charge > 0.5, "Mantener Espacio carga el lanzador original")
	key(KEY_SPACE, false); await frames(5)
	check(not table._charging, "Soltar Espacio libera el lanzador")
	key(KEY_P, true); key(KEY_P, false); await frames(3)
	check(get_tree().paused, "P pausa el juego dentro del alojamiento")
	var position := table.ball.global_position
	var rain: float = Drifft.ambient_time
	await frames(15)
	check(table.ball.global_position.is_equal_approx(position), "La bola no se mueve durante la pausa")
	check(Drifft.ambient_time > rain, "El ambiente sigue animado durante la pausa")
	key(KEY_P, true); key(KEY_P, false); await frames(3)
	check(not get_tree().paused, "P reanuda la partida")
	Flow.level_finished(false); await frames(4)
	check(Flow.beat == Flow.Beat.LOSE and Flow.chapter_index == 0, "Perder muestra la escena original de derrota del mismo capítulo")
	await advance_to_level(0)
	check(Drifft.game_scene.balls_left == 3, "Reintentar recupera los tres ovillos del primer capítulo")
	Flow.level_finished(true); await frames(3); await advance_to_level(1)
	check(Drifft.game_scene.scene_file_path == "res://scenes/level2.tscn", "La progresión carga el segundo nivel original")
	check(Drifft.game_scene.balls_left == 4, "Conserva los cuatro ovillos del segundo nivel")
	Flow.level_finished(true); await frames(3); await advance_to_level(2)
	check(Drifft.game_scene.scene_file_path == "res://scenes/level3.tscn", "La progresión carga el tercer nivel original")
	check(Drifft.game_scene.balls_left == 5, "Conserva los cinco ovillos del nivel final")
	Flow.level_finished(true); await frames(4)
	for i in 150:
		await frames(2)
		if Flow.beat == Flow.Beat.ENDING:
			break
		if Drifft.game_scene.has_method("_on_confirm"):
			Drifft.game_scene._on_confirm()
	check(Flow.beat == Flow.Beat.ENDING, "La campaña conserva el final original traducido")
	check(Flow.campaign.ending_title == "La defensa de los gatos", "El título final está en español")
	print("SUPERADAS ", checks, " comprobaciones de integración Godot")
	get_tree().quit(0)
