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
uniform(vec2, screenSize, ScreenSize);
uniform(vec3, cameraPosition, CameraPosition);

void main()
{
	vec2 project = gl_FragCoord.xy / screenSize.xy;
	
	vec3 normal = normalize(texture2D(samplerNormal, project).xyz);
	vec3 position = texture2D(samplerPosition, project).xyz;
	
	vec3 lightDir = lightPosition - position;
	float dist = length(lightDir);
	lightDir = lightDir / dist;
	
	float nDotL = dot(normal, lightDir);
	
	vec3 eyeDir = cameraPosition - position;
	vec3 H = normalize(eyeDir + lightDir);
	
	float nDotH = saturate(dot(normal, H));
	float specular = pow(nDotH, 16);
	
	float attenuation = dist / lightRange;
	attenuation = saturate(1.0f - (attenuation * attenuation));
	
	oColor = vec4((lightColor * (nDotL + specular.xxx)) * attenuation, 1.0f);
}
#endif