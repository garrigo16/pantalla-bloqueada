shader_type canvas_item;
render_mode blend_add;
// Autor: Miquel Garrigo
uniform float clock = 0.0;
void fragment() {
	vec2 p = (UV - vec2(0.5, 0.42)) * vec2(1.0, 0.9);
	float halo = exp(-dot(p, p) * 13.0);
	float breath = 0.11 + 0.035 * sin(clock * 1.05) + 0.008 * sin(clock * 2.37);
	COLOR = vec4(1.0, 0.66, 0.27, halo * breath);
}
