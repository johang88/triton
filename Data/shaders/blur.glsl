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

sampler(2D, samplerScene, SceneTexture);
uniform(vec4[15], sampleOffsets, SampleOffsets);
uniform(vec4[15], sampleWeights, SampleWeights);

void main()
{
	vec4 sum = vec4(0.0);

	for (int i = 0; i < 15; i++)
	{
		vec2 uv = texCoord + sampleOffsets[i].xy;
		sum += sampleWeights[i] * texture2D(samplerScene, uv); 
	}
	
	oColor = sum;
	oColor.a = 1.0f;
}
#endif