shader_type canvas_item;
// Autor: Miquel Garrigo
void fragment() {
	vec2 p = UV * vec2(440.0, 660.0);
	float eye1 = length((p - vec2(337.0, 75.0)) / vec2(13.0, 16.0));
	float eye2 = length((p - vec2(364.0, 68.0)) / vec2(14.0, 16.0));
	vec4 closed_eyes = texture(TEXTURE, UV);
	COLOR = vec4(closed_eyes.rgb, closed_eyes.a * COLOR.a * (1.0 - smoothstep(0.9, 1.0, min(eye1, eye2))));
}
