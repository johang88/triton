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
sampler(2D, samplerBloom, BloomTexture);

uniform(float, exposure, Exposure);
uniform(vec3, whitePoint, WhitePoint);

vec3 tonemap(vec3 x)
{
	float A = 0.15;
	float B = 0.50;
	float C = 0.10;
	float D = 0.20;
	float E = 0.02;
	float F = 0.30;
	
	return ((x*(A*x+C*B)+D*E)/(x*(A*x+B)+D*F))-E/F;
}

void main()
{
	vec3 scene = texture2D(samplerScene, texCoord).xyz;
	vec3 bloom = texture2D(samplerBloom, texCoord).xyz;

	vec3 final = tonemap(scene * exposure) / tonemap(whitePoint);
	final += bloom;
	
	final = pow(final, (1.0f / 2.2f).xxx);
	
	oColor = vec4(final, 1.0f);
}
#endif