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

import(shaders/utility/utils);

in vec2 texCoord;

out(vec4, oColor, 0);

sampler(2D, samplerDiffuse, DiffuseTexture);
sampler(2D, samplerNormal, NormalTexture);
sampler(2D, samplerPosition, PositionTexture);

void main()
{
	vec3 diffuse = texture2D(samplerDiffuse, texCoord).xyz;
	vec3 normal = texture2D(samplerNormal, texCoord).xyz;
	vec3 position = texture2D(samplerPosition, texCoord).xyz;

	diffuse = mix(diffuse, vec3(1, 1, 1), saturate((position.z - 1) / (30 - 1)));
	
	oColor = vec4(diffuse.xyz, 1.0f);
}
#endif