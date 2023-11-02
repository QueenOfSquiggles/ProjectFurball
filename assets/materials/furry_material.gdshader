shader_type spatial;
render_mode cull_back, depth_draw_always;
// render_mode alpha_to_coverage;// cull_disabled;//, depth_draw_always;//, alpha_to_coverage_and_one;

uniform float density = 16.0;
uniform float taper_power = 2.0;
uniform bool test_geometry = false;

// a gradient 1D texture where the red channel is the alpha of the strand along the length
uniform sampler2D taper_alpha : hint_default_white;

instance uniform float shell_depth;
instance uniform vec4 base_col = vec4(0.0);
instance uniform vec4 tip_col = vec4(1.0);

// Gold Noise ©2015 dcerisano@standard3d.com
// - based on the Golden Ratio
// - uniform normalized distribution
// - fastest static noise generator function (also runs at low precision)
// - use with indicated fractional seeding method. 
varying vec2 hashing_uv;

float hash12(vec2 p)
{
	vec3 p3  = fract(vec3(p.xyx) * .1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return fract((p3.x + p3.y) * p3.z);
}


void fragment() {
	ALBEDO = mix(base_col.rgb, tip_col.rgb, shell_depth);
	ALPHA = texture(taper_alpha, vec2(shell_depth, 0)).r;
	if (!test_geometry) {
		vec2 hashUV = floor(UV * density);
		vec2 cellUV = abs(fract((UV * density) - hashUV) - 0.5) * 2.0;
		float hval = hash12(hashUV);
		float dtaper = pow(1.0 - shell_depth, taper_power);
		if (length(cellUV) >= dtaper || hval <= shell_depth) {
			discard;
		}
	}
}