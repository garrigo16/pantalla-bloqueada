extends Node
# Autor: Miquel Garrigo

var checks := 0
func _ready() -> void:
	_run.call_deferred()

func frames(count: int) -> void:
	for i in count:
		await get_tree().process_frame

func check(ok: bool, message: String) -> void:
	if not ok:
		push_error(message)
		get_tree().quit(1)
		assert(ok, message)
	checks += 1
	print("OK ", message)

func _run() -> void:
	Drifft.begin()
	await frames(5)
	var font: FontVariation = Drifft.TEXT_FONT
	var title_font: FontVariation = Drifft.TITLE_FONT
	var ts := TextServerManager.get_primary_interface()
	var weight := ts.name_to_tag("wght")
	print("FONT ", font.get_font_name(), " AXES ", font.get_supported_variation_list(), " RID ", ts.font_get_variation_coordinates(font.get_rids()[0]))
	check(font.base_font is FontFile and font.base_font.resource_path.ends_with("Nunito.ttf"), "Nunito se carga embebida sin depender de las fuentes de Windows")
	check(ts.font_get_variation_coordinates(font.get_rids()[0]).get(weight) == 500.0, "El motor renderiza el peso Medium 500 real del texto")
	check(ts.font_get_variation_coordinates(title_font.get_rids()[0]).get(weight) == 700.0, "El motor renderiza el peso Bold 700 real de los títulos")
	var spanish := true
	for character in "áéíóúüñÁÉÍÓÚÜÑ¿¡…":
		spanish = spanish and font.has_char(character.unicode_at(0))
	check(spanish, "La fuente cubre tildes, diéresis, eñe y signos del español")
	Flow.start()
	await frames(5)
	var story: Node = Drifft.game_scene
	var paragraphs: Array = []
	for chapter in Flow.campaign.chapters:
		for part in ["intro", "outro", "lose"]:
			paragraphs.append_array(chapter.get(part, []))
	paragraphs.append_array(Flow.campaign.get("ending", []))
	var body: Label = story.get_node("Box/Lines/Body")
	var overflow: Array = []
	for paragraph in paragraphs:
		story._lines = [paragraph]
		story._i = -1
		story._advance_line()
		Drifft._style_text(story)
		await frames(1)
		var required: float = body.get_line_count() * body.get_line_height() + max(0, body.get_line_count() - 1) * body.get_theme_constant("line_spacing")
		if required > body.size.y + 1:
			overflow.append(body.text)
	print("PARAGRAPHS ", paragraphs.size(), " OVERFLOW ", overflow)
	check(overflow.is_empty(), "Todos los párrafos de la campaña caben con la letra ampliada")
	for chapter in Flow.campaign.chapters:
		var label: Label = story.get_node("Title")
		label.text = chapter.title
		Drifft._style_text(story)
		var text_width := title_font.get_string_size(label.text, HORIZONTAL_ALIGNMENT_LEFT, -1, label.get_theme_font_size("font_size")).x
		check(text_width <= 648, "El título completo del capítulo cabe sin recortes: " + chapter.title)
	check(Drifft.viewport.size_2d_override == Vector2i(720, 1280), "La mejora de definición conserva el espacio lógico del juego")
	print("SUPERADAS ", checks, " comprobaciones de tipografía")
	get_tree().quit()
