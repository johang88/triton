import(shaders/core);
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

uniform sampler2D samplerPosition;
uniform sampler2D samplerNormal;
uniform sampler2D samplerRandom;

uniform vec2 noiseScale;

float bias = 0.55;
float intensity = 30.0;
float scale = 5.0;
float radius = 0.1;

float ambientOcclusion(vec2 tc, vec2 uv, vec3 p, vec3 norm) {
	vec3 diff = texture2D(samplerPosition, tc + uv).xyz - p;
	vec3 v = normalize(diff);
	float d = length(diff) * scale;
	
	return max(0.0, dot(norm, v) - bias) * (1.0 / (1.0 + d)) * intensity;
}

void main()
{
	vec3 position = texture2D(samplerPosition, texCoord).xyz;
	vec3 normal = normalize(texture2D(samplerNormal, texCoord).xyz);
	
	vec3 rand = texture2D(samplerRandom, texCoord * noiseScale).xyz * 2.0 - 1.0;
	
	float rad = radius / position.z;

	const vec2 vec[4] = { vec2(1, 0), vec2(-1, 0), vec2(0, 1), vec2(0, -1) };

	float occlusion = 0.0;
	int iterations = 4;
	for (int i = 0; i < iterations; i++) {
		vec2 coord1 = (reflect(vec3(vec[i], 0), rand) * rad).xy;
		vec2 coord2 = vec2(coord1.x * 0.707 - coord1.y * 0.707, coord1.x * 0.707 + coord1.y * 0.707);
		
		occlusion += ambientOcclusion(texCoord, coord1 * 0.25, position, normal);
		occlusion += ambientOcclusion(texCoord, coord2 * 0.5, position, normal);
		occlusion += ambientOcclusion(texCoord, coord1 * 0.75, position, normal);
		occlusion += ambientOcclusion(texCoord, coord2 * 1.0, position, normal);
	}
	
	occlusion = max(0, 1.0 - occlusion / (iterations * 4.0));
	
	oColor = vec4(vec3(occlusion), 1.0);
}
#endif