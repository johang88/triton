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
import(shaders/lighting/phong);

in vec2 texCoord;

out(vec4, oColor, 0);

sampler(2D, samplerNormal, NormalTexture);
sampler(2D, samplerPosition, PositionTexture);
sampler(2D, samplerSpecular, SpecularTexture);
sampler(2D, samplerDiffuse, DiffuseTexture);

uniform(vec3, lightPosition, LightPosition);
uniform(vec3, lightColor, LightColor);
uniform(vec2, spotParams, SpotLightParams);
uniform(float, lightRange, LightRange);
uniform(vec2, screenSize, ScreenSize);
uniform(vec3, cameraPosition, CameraPosition);
uniform(vec3, lightDirection, LightDirection);

void main()
{
	vec2 project = gl_FragCoord.xy / screenSize.xy;
	
	vec3 normal = normalize(texture2D(samplerNormal, project).xyz);
	vec4 specularColor = texture2D(samplerSpecular, project);
	vec3 diffuse = texture2D(samplerDiffuse, project).xyz;
	vec3 position = texture2D(samplerPosition, project).xyz;
	
	float specularPower = 128 * specularColor.w;
	
	vec3 lightDir = lightPosition - position;
	float dist = length(lightDir);
	lightDir = lightDir / dist;
	
	vec3 eyeDir = normalize(cameraPosition - position);
	
	float attenuation = dist / lightRange;
	attenuation = saturate(1.0f - (attenuation * attenuation));
	
	float spotLightAngle = saturate(dot(-lightDirection, lightDir));
	float spotFallof = 1.0f - saturate((spotLightAngle - spotParams.x) / (spotParams.y - spotParams.x));
	
	oColor.xyz = phong(normal, eyeDir, lightDir, specularPower, lightColor, diffuse, specularColor.xyz, attenuation * spotFallof);
	oColor.w = 1.0f;
}
#endif