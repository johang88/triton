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
sampler(2D, samplerShadow, ShadowMap);

uniform(vec3, lightPosition, LightPosition);
uniform(vec3, lightColor, LightColor);
uniform(vec2, spotParams, SpotLightParams);
uniform(float, lightRange, LightRange);
uniform(vec2, screenSize, ScreenSize);
uniform(vec3, cameraPosition, CameraPosition);
uniform(vec3, lightDirection, LightDirection);
uniform(mat4x4, invView, InverseViewMatrix);
uniform(mat4x4, shadowViewProj, ShadowViewProjection);
uniform(float, inverseShadowMapSize, InverseShadowMapSize);

float check_shadow(sampler2D shadowMap, vec3 viewPos, mat4x4 invView, mat4x4 shadowViewProj, float texelSize) {
	vec3 worldPos = (invView * vec4(viewPos, 1)).xyz;
	vec4 shadowUv = shadowViewProj * vec4(worldPos, 1);

	vec2 jitterFactor = fract(gl_FragCoord.xy * vec2(18428.4f, 23614.3f)) * 2.0f - 1.0f;
	vec2 o = texelSize.xx * 0.93f * jitterFactor;

	vec2 uv = 0.5f * shadowUv.xy / shadowUv.w + vec2(0.5f, 0.5f);
	
	float distance = (shadowUv.z / 100.0f) - 0.001f;
	
	float c = step(distance, texture2D(shadowMap, uv.xy).x);
	c += step(distance, texture2D(shadowMap, uv.xy - o.xy).x);
	c += step(distance, texture2D(shadowMap, uv.xy + o.xy).x);
	c += step(distance, texture2D(shadowMap, vec2(uv.x - o.x, uv.y)).x);
	c += step(distance, texture2D(shadowMap, vec2(uv.x + o.x, uv.y)).x);
	c += step(distance, texture2D(shadowMap, vec2(uv.x, uv.y + o.y)).x);
	c += step(distance, texture2D(shadowMap, vec2(uv.x, uv.y - o.y)).x);
	c += step(distance, texture2D(shadowMap, vec2(uv.x - o.x, uv.y + o.y)).x);
	c += step(distance, texture2D(shadowMap, vec2(uv.x + o.x, uv.y - o.y)).x);
	
	return c / 9.0f;
}

void main()
{
	vec2 project = gl_FragCoord.xy / screenSize.xy;
	
	vec3 normal = normalize(texture2D(samplerNormal, project).xyz);
	vec4 specularColor = texture2D(samplerSpecular, project);
	vec3 diffuse = texture2D(samplerDiffuse, project).xyz;
	vec3 position = texture2D(samplerPosition, project).xyz;
	
	float specularPower = 32 * specularColor.w;
	
	vec3 lightDir = lightPosition - position;
	float dist = length(lightDir);
	lightDir = lightDir / dist;
	
	vec3 eyeDir = normalize(cameraPosition - position);
	
	float attenuation = dist / lightRange;
	attenuation = saturate(1.0f - (attenuation * attenuation));
	
	float spotLightAngle = saturate(dot(-lightDirection, lightDir));
	float spotFallof = 1.0f - saturate((spotLightAngle - spotParams.x) / (spotParams.y - spotParams.x));
	
	float shadow = check_shadow(samplerShadow, position, invView, shadowViewProj, inverseShadowMapSize);
	
	oColor.xyz = phong(normal, eyeDir, lightDir, specularPower, lightColor, diffuse, specularColor.xyz, attenuation * spotFallof) * shadow;
	oColor.w = 1.0f;
}
#endif