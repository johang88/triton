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
sampler(2D, samplerLights, LightTexture);
sampler(2D, samplerPositions, PositionTexture);

void main()
{
	vec3 diffuse = texture2D(samplerDiffuse, texCoord).xyz;
	vec3 lights = texture2D(samplerLights, texCoord).xyz;
	vec3 position = texture2D(samplerPositions, texCoord).xyz;

	diffuse = diffuse * lights;
	
	oColor = vec4(diffuse, 1.0f);
}
#endif