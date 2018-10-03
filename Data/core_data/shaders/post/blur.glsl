#include "/shaders/core"
#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_TEXCOORD_0) in vec2 iTexCoord;

out vec2 texCoord;

void main()
{
	texCoord = iTexCoord;
	gl_Position = vec4(iPosition, 1);
}

#else

in vec2 texCoord;

layout(location = 0) out vec4 oColor;

uniform sampler2D samplerScene;
uniform vec2 textureSize;
uniform float blurSigma;

float calc_gaussian_weight(float sampleDistance, float sigma) {
	float g = 1.0 / sqrt(2.0 * 3.14159 * sigma * sigma);
	return (g - exp(-(sampleDistance * sampleDistance) / (2 * sigma * sigma)));
}

vec4 blur(vec2 texScale, float sigma) {
	vec4 color = vec4(0);
	for (int i = -6; i < 6; i++) {
		float weight = calc_gaussian_weight(i, sigma);
		vec2 tc = texCoord + (vec2(i) / textureSize) * texScale;
		vec4 s = texture(samplerScene, tc);
		color += s * weight;
	}
	
	return color;
}

vec4 blur9(sampler2D image, vec2 uv, vec2 resolution, vec2 direction) {
	vec4 color = vec4(0.0);
	vec2 off1 = vec2(1.3846153846) * direction;
	vec2 off2 = vec2(3.2307692308) * direction;
	color += texture2D(image, uv) * 0.2270270270;
	color += texture2D(image, uv + (off1 / resolution)) * 0.3162162162;
	color += texture2D(image, uv - (off1 / resolution)) * 0.3162162162;
	color += texture2D(image, uv + (off2 / resolution)) * 0.0702702703;
	color += texture2D(image, uv - (off2 / resolution)) * 0.0702702703;
	return color;
}

vec4 blur13(sampler2D image, vec2 uv, vec2 resolution, vec2 direction) {
	vec4 color = vec4(0.0);
	vec2 off1 = vec2(1.411764705882353) * direction;
	vec2 off2 = vec2(3.2941176470588234) * direction;
	vec2 off3 = vec2(5.176470588235294) * direction;
	color += texture2D(image, uv) * 0.1964825501511404;
	color += texture2D(image, uv + (off1 / resolution)) * 0.2969069646728344;
	color += texture2D(image, uv - (off1 / resolution)) * 0.2969069646728344;
	color += texture2D(image, uv + (off2 / resolution)) * 0.09447039785044732;
	color += texture2D(image, uv - (off2 / resolution)) * 0.09447039785044732;
	color += texture2D(image, uv + (off3 / resolution)) * 0.010381362401148057;
	color += texture2D(image, uv - (off3 / resolution)) * 0.010381362401148057;
	return color;
}


void main()
{
	vec4 sum = vec4(0.0);
	
#ifdef BLUR_HORIZONTAL
	oColor = blur9(samplerScene, texCoord, textureSize, vec2(1, 0));
#else
	oColor = blur9(samplerScene, texCoord, textureSize, vec2(0, 1));
#endif
}
#endif