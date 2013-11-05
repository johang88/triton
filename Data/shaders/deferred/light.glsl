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

#ifdef SHADOWS
import(shaders/deferred/shadows);
#endif

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

#ifdef SHADOWS
uniform(mat4x4, invView, InverseViewMatrix);
sampler(2DShadow, samplerShadow, ShadowMap);
uniform(mat4x4, shadowViewProj, ShadowViewProjection);
uniform(vec2, clipPlane, ClipPlane);
uniform(float, shadowBias, ShadowBias);
#endif

void main()
{
	vec2 project = gl_FragCoord.xy / screenSize.xy;
	
	vec3 normal = normalize(texture2D(samplerNormal, project).xyz);
	vec4 specularColor = texture2D(samplerSpecular, project);
	vec3 diffuse = texture2D(samplerDiffuse, project).xyz;
	vec3 position = texture2D(samplerPosition, project).xyz;
	
	float specularPower = 32 * specularColor.w;
	
#if defined(SPOT_LIGHT) || defined(POINT_LIGHT)
	vec3 lightDir = lightPosition - position;
	float dist = length(lightDir);
	lightDir = lightDir / dist;
#else
	vec3 lightDir = -normalize(lightDirection);
#endif
	
	vec3 eyeDir = normalize(cameraPosition - position);
	
#if defined(SPOT_LIGHT) || defined(POINT_LIGHT)
	float attenuation = dist / lightRange;
	attenuation = saturate(1.0f - (attenuation * attenuation));
#else
	float attenuation = 1.0f;
#endif
	
#ifdef SPOT_LIGHT
	float spotLightAngle = saturate(dot(-lightDirection, lightDir));
	float spotFallof = 1.0f - saturate((spotLightAngle - spotParams.x) / (spotParams.y - spotParams.x));
	
	attenuation *= spotFallof;
#endif
	
#ifdef SHADOWS
	float shadow = check_shadow(samplerShadow, position, invView, shadowViewProj, clipPlane, shadowBias);
#else
	float shadow = 1.0f;
#endif
	
	oColor.xyz = phong(normal, eyeDir, lightDir, specularPower, lightColor, diffuse, specularColor.xyz, attenuation) * shadow;
	oColor.w = 1.0f;
}
#endif