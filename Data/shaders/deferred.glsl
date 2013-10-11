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

sampler(2D, samplerNormal, NormalTexture);
sampler(2D, samplerPosition, PositionTexture);

uniform(vec3, lightPosition, LightPosition);
uniform(vec3, lightColor, LightColor);
uniform(float, lightRange, LightRange);

void main()
{
	vec3 normal = normalize(texture2D(samplerNormal, texCoord).xyz);
	vec3 position = texture2D(samplerPosition, texCoord).xyz;
	
	vec3 lightDir = lightPosition - position;
	
	float nDotL = dot(normal, normalize(lightDir));
	
	float attenuation = length(lightDir) / lightRange;
	attenuation = saturate(1.0f - (attenuation * attenuation));
	
	nDotL = nDotL * attenuation;

	oColor = vec4(lightColor * nDotL, 1.0f);
}
#endif