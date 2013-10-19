#ifdef VERTEX_SHADER

attrib(vec3, iPosition, Position);
attrib(vec2, iTexCoord, TexCoord);

out vec2 texCoord;

uniform(mat4x4, modelViewProjection, ModelViewProjection);

void main()
{
	texCoord = iTexCoord;
	gl_Position = modelViewProjection * vec4(iPosition, 1);
}

#else

in vec2 texCoord;

out(vec4, oColor, 0);

sampler(2D, samplerPosition, PositionTexture);
sampler(2D, samplerNormal, NormalTexture);
sampler(2D, samplerRandom, RandomTexture);

uniform(vec2, noiseScale, NoiseScale);

float bias = 0.55f;
float intensity = 30.0f;
float scale = 5.0f;
float radius = 0.1f;

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
	
	vec3 rand = texture2D(samplerRandom, texCoord * noiseScale).xyz * 2.0f - 1.0f;
	
	float rad = radius / position.z;

	const vec2 vec[4] = { vec2(1, 0), vec2(-1, 0), vec2(0, 1), vec2(0, -1) };

	float occlusion = 0.0f;
	int iterations = 4;
	for (int i = 0; i < iterations; i++) {
		vec2 coord1 = reflect(vec[i], rand) * rad;
		vec2 coord2 = vec2(coord1.x * 0.707f - coord1.y * 0.707f, coord1.x * 0.707f + coord1.y * 0.707f);
		
		occlusion += ambientOcclusion(texCoord, coord1 * 0.25f, position, normal);
		occlusion += ambientOcclusion(texCoord, coord2 * 0.5f, position, normal);
		occlusion += ambientOcclusion(texCoord, coord1 * 0.75f, position, normal);
		occlusion += ambientOcclusion(texCoord, coord2 * 1.0f, position, normal);
	}
	
	occlusion = max(0, 1.0f - occlusion / (iterations * 4.0f));
	
	oColor = vec4(occlusion.xxx, 1.0f);
}
#endif