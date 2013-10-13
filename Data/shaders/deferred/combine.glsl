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
sampler(2D, samplerSpecular, SpecularTexture);

uniform(float, exposure, Exposure);

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
	vec3 diffuse = texture2D(samplerDiffuse, texCoord).xyz;
	vec3 light = texture2D(samplerLights, texCoord).xyz;
	vec3 specular = texture2D(samplerSpecular, texCoord).xyz;

	diffuse = diffuse * light + specular;
	
	// diffuse = tonemap(diffuse * exposure)/* / tonemap(vec3(10.0f, 10.0f, 10.0f))*/;
	
	// diffuse = pow(diffuse, (1.0f / 2.2f).xxx);
	
	oColor = vec4(diffuse, 1.0f);
}
#endif