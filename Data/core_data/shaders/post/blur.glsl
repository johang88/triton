import(/shaders/core);
#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_TEXCOORD_0) in vec2 iTexCoord;

out vec2 texCoord;

uniform mat4x4 modelViewProjection;

void main()
{
	texCoord = iTexCoord;
	gl_Position = modelViewProjection * vec4(iPosition, 1);
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
		vec4 s = texture2D(samplerScene, tc);
		color += s * weight;
	}
	
	return color;
}

void main()
{
	vec4 sum = vec4(0.0);
	
#ifdef BLUR_HORIZONTAL
	oColor = blur(vec2(1, 0), blurSigma);
#else
	oColor = blur(vec2(0, 1), blurSigma);
#endif
}
#endif