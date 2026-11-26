extends Node
# Autor: Miquel Garrigo

var viewport: SubViewport
var game_scene: Node
var container: SubViewportContainer
var background: Control
var eyes: TextureRect
var rain: Node2D
var lamp: ColorRect
var last_input := 0
var next_blink := 2.8
var blink_time := -1.0
var double_blink := false
var ambient_time := 0.0
var status_path := ""
var status_at := 0.0
var chapter_label: Label
const TEXT_FONT = preload("res://drifft/font.tres")
const TITLE_FONT = preload("res://drifft/font-title.tres")
const GAME_SIZE := Vector2(320, 570)

func begin() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	# La misma fuente embebida en etiquetas y textos dibujados por el juego.
	ThemeDB.fallback_font = TEXT_FONT
	last_input = Time.get_ticks_msec()
	var args := OS.get_cmdline_user_args()
	for i in args.size() - 1:
		if args[i] == "--drifft-status":
			status_path = args[i + 1]
	AudioServer.set_bus_mute(0, true)
	if DisplayServer.get_name() != "headless":
		DisplayServer.window_set_flag(DisplayServer.WINDOW_FLAG_BORDERLESS, true)
		DisplayServer.window_set_flag(DisplayServer.WINDOW_FLAG_ALWAYS_ON_TOP, true)
	for action in ["flip_left", "flip_right"]:
		InputMap.action_erase_events(action)
		var key := InputEventKey.new()
		key.physical_keycode = KEY_CTRL
		key.location = KEY_LOCATION_LEFT if action == "flip_left" else KEY_LOCATION_RIGHT
		InputMap.action_add_event(action, key)
	var right_arrow := InputEventKey.new()
	right_arrow.physical_keycode = KEY_RIGHT
	InputMap.action_add_event("flip_right", right_arrow)
	background = Control.new()
	background.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(background)
	var image := NinePatchRect.new()
	var art: Image = load("res://drifft/pinball-nook.png").get_image()
	art.resize(440, 660, Image.INTERPOLATE_LANCZOS)
	image.texture = ImageTexture.create_from_image(art)
	image.size = Vector2(440, 800)
	image.patch_margin_left = 60
	image.patch_margin_right = 60
	image.patch_margin_top = 132
	image.patch_margin_bottom = 45
	image.mouse_filter = Control.MOUSE_FILTER_IGNORE
	background.add_child(image)
	rain = Node2D.new()
	rain.set_script(load("res://drifft/rain.gd"))
	background.add_child(rain)
	lamp = ColorRect.new()
	lamp.position = Vector2(181, 0)
	lamp.size = Vector2(116, 116)
	lamp.mouse_filter = Control.MOUSE_FILTER_IGNORE
	lamp.material = ShaderMaterial.new()
	lamp.material.shader = load("res://drifft/lamp.gdshader")
	background.add_child(lamp)
	eyes = TextureRect.new()
	eyes.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	eyes.texture = load("res://drifft/pinball-blink.png")
	eyes.size = Vector2(440, 660)
	eyes.mouse_filter = Control.MOUSE_FILTER_IGNORE
	eyes.modulate.a = 0
	eyes.material = ShaderMaterial.new()
	eyes.material.shader = load("res://drifft/blink.gdshader")
	background.add_child(eyes)
	_label("El rincón del gato", Vector2(40, 126), Vector2(360, 30), 20)
	chapter_label = _label("", Vector2(60, 153), Vector2(320, 19), 11)
	_label("Ctrl izquierdo", Vector2(60, 762), Vector2(160, 24), 11)
	_label("Ctrl derecho o →", Vector2(220, 762), Vector2(160, 24), 11)
	_label("Espacio · Cargar y soltar    P · Pausa    Esc · Volver", Vector2(8, 812), Vector2(424, 24), 12)
	_label("Una aventura entre libros y lluvia.", Vector2(10, 838), Vector2(420, 22), 11)
	container = SubViewportContainer.new()
	container.set_script(load("res://drifft/input_bridge.gd"))
	container.position = Vector2(60, 178)
	container.size = Vector2(320, 570)
	container.stretch = true
	container.stretch_shrink = 1
	container.mouse_filter = Control.MOUSE_FILTER_STOP
	add_child(container)
	viewport = SubViewport.new()
	viewport.size = Vector2i(720, 1280)
	viewport.size_2d_override = Vector2i(720, 1280)
	viewport.size_2d_override_stretch = true
	viewport.transparent_bg = true
	viewport.handle_input_locally = true
	viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	viewport.process_mode = Node.PROCESS_MODE_PAUSABLE
	container.add_child(viewport)
	_sync_resolution()
	Juice.reparent(viewport)
	change_game_scene("res://scenes/start.tscn")

func _label(value: String, at: Vector2, dimensions: Vector2, font_size: int) -> Label:
	var label := Label.new()
	label.text = value
	label.position = at
	label.size = dimensions
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.add_theme_font_size_override("font_size", font_size)
	label.add_theme_font_override("font", TITLE_FONT if font_size >= 20 else TEXT_FONT)
	label.add_theme_color_override("font_color", Color("f8e1b6"))
	label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(label)
	return label

func _sync_resolution() -> void:
	# Renderizar a los píxeles finales evita ampliar una textura de texto pequeña.
	# El espacio lógico y la física siguen siendo de 720 × 1280.
	var screen_scale := get_viewport().get_screen_transform().get_scale().abs()
	var pixels := Vector2i((GAME_SIZE * screen_scale).round()).max(Vector2i.ONE)
	if viewport.size != pixels:
		container.size = Vector2(pixels)
		container.scale = GAME_SIZE / Vector2(pixels)

func change_game_scene(path: String) -> void:
	_change.call_deferred(path)

func _change(path: String) -> void:
	get_tree().paused = false
	_release()
	if is_instance_valid(game_scene):
		viewport.remove_child(game_scene)
		game_scene.queue_free()
	game_scene = load(path).instantiate()
	viewport.add_child(game_scene)
	_style_text(game_scene)
	chapter_label.visible = game_scene is Table
	if game_scene is Table:
		chapter_label.text = str(Flow.current_chapter().get("title", ""))
		game_scene.chapter_title = ""
		game_scene.get_node("UI/Hint").hide()
	_write_status()

func _style_text(node: Node) -> void:
	if node is Label and node.name == "Title":
		node.add_theme_font_override("font", TITLE_FONT)
		var width := TITLE_FONT.get_string_size(node.text, HORIZONTAL_ALIGNMENT_LEFT, -1, 36).x
		node.add_theme_font_size_override("font_size", mini(36, int(36 * 648 / maxf(width, 1))))
	elif node is Label and node.name == "Body":
		node.add_theme_font_size_override("font_size", 30)
	elif node is Label and node.name == "Act":
		node.add_theme_font_size_override("font_size", 24)
		node.add_theme_color_override("font_color", Color("c8baa6"))
	elif node is Label and node.name == "Who":
		node.add_theme_font_size_override("font_size", 28)
	elif node is Label and node.name == "Hint":
		node.add_theme_font_size_override("font_size", 23)
		node.add_theme_color_override("font_color", Color("dbc5a8"))
	for child in node.get_children():
		_style_text(child)

func _input(event: InputEvent) -> void:
	if viewport == null:
		return
	if event is InputEventKey:
		last_input = Time.get_ticks_msec()
		var key := event as InputEventKey
		if key.pressed and not key.echo and key.physical_keycode == KEY_ESCAPE:
			get_tree().quit()
			return
		# Los atajos globales pertenecen a Windows y a Drifft Analyzer.
		if key.meta_pressed or key.alt_pressed:
			return
		get_viewport().set_input_as_handled()
		viewport.push_input(event, true)
	elif event is InputEventMouseButton or event is InputEventMouseMotion:
		last_input = Time.get_ticks_msec()

func _notification(what: int) -> void:
	if what == NOTIFICATION_APPLICATION_FOCUS_OUT:
		_release()
		if is_instance_valid(game_scene) and game_scene.has_method("toggle_pause") and not get_tree().paused:
			game_scene.toggle_pause()

func _release() -> void:
	for action in ["flip_left", "flip_right", "launch_ball"]:
		Input.action_release(action)
	if is_instance_valid(game_scene) and game_scene is Table:
		game_scene._charging = false
		game_scene._charge = 0.0
		game_scene._wait_release = true

func _process(delta: float) -> void:
	if viewport == null:
		return
	# Tiempo real: el parpadeo y la lluvia siguen suaves durante el hitstop.
	_sync_resolution()
	var real_delta := delta / maxf(Engine.time_scale, 0.001)
	ambient_time += real_delta
	lamp.material.set_shader_parameter("clock", ambient_time)
	next_blink -= real_delta
	if next_blink <= 0:
		blink_time = 0
		double_blink = randf() < 0.3
		next_blink = randf_range(3.2, 7.0)
	if blink_time >= 0:
		blink_time += real_delta
		var t := blink_time
		if double_blink and t > 0.42:
			t -= 0.42
		eyes.modulate.a = clampf(t / 0.08, 0, 1) if t < 0.17 else clampf((0.29 - t) / 0.12, 0, 1)
		if blink_time > (0.71 if double_blink else 0.29):
			blink_time = -1
			eyes.modulate.a = 0
	if Time.get_ticks_msec() - last_input > 60000:
		get_tree().quit()
	status_at += real_delta
	if status_at > 0.2:
		status_at = 0
		_write_status()

func _write_status() -> void:
	if status_path.is_empty():
		return
	var state := {"chapter": Flow.chapter_index, "beat": int(Flow.beat), "paused": get_tree().paused,
		"left": Input.is_action_pressed("flip_left"), "right": Input.is_action_pressed("flip_right"),
		"input_at": last_input, "rain": ambient_time, "blink": eyes.modulate.a,
		"scene": game_scene.scene_file_path if is_instance_valid(game_scene) else ""}
	var file := FileAccess.open(status_path, FileAccess.WRITE)
	if file != null:
		file.store_string(JSON.stringify(state))
