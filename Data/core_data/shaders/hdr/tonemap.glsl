import(shaders/core);
#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_TEXCOORD_0) in vec2 iTexCoord;

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

layout(location = 0) out vec4 oColor;

sampler(2D, samplerScene, SceneTexture);
sampler(2D, samplerBloom, BloomTexture);

uniform(float, exposure, Exposure);
uniform(vec3, whitePoint, WhitePoint);

vec3 tonemap(vec3 x)
{
	float A = 0.15f;
	float B = 0.50f;
	float C = 0.10f;
	float D = 0.20f;
	float E = 0.02f;
	float F = 0.30f;
	
	return ((x*(A*x+C*B)+D*E)/(x*(A*x+B)+D*F))-E/F;
}

void main()
{
	vec3 scene = texture2D(samplerScene, texCoord).xyz;
	vec3 bloom = texture2D(samplerBloom, texCoord).xyz;

	scene = tonemap(scene * exposure);
	vec3 whiteScale = 1.0f / tonemap(whitePoint);
	
	vec3 final = scene * whiteScale;
	final += bloom;
	
	final = pow(final, (1.0f / 2.2f).xxx);
	
	oColor = vec4(final, 1.0f);
}
#endif