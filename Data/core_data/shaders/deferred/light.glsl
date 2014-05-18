import(shaders/core);
#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_TEXCOORD_0) in vec2 iTexCoord;

out vec2 texCoord;

uniform mat4x4 modelViewProjection;

void main()
{
	texCoord = iTexCoord;
	
#if defined(SPOT_LIGHT) || defined(POINT_LIGHT)
	gl_Position = modelViewProjection * vec4(iPosition, 1);
#else
	gl_Position = vec4(iPosition, 1);
#endif
}

#else

import(shaders/utility/utils);
import(shaders/deferred/brdf);
import(shaders/deferred/shadows);

in vec2 texCoord;

layout(location = 0) out vec4 oColor;

uniform sampler2D samplerNormal;
uniform sampler2D samplerPosition;
uniform sampler2D samplerSpecular;
uniform sampler2D samplerDiffuse;

uniform vec3 lightPosition;
uniform vec3 lightColor;
uniform vec2 spotParams;
uniform float lightRange;
uniform vec2 screenSize;
uniform vec3 cameraPosition;
uniform vec3 lightDirection;
uniform mat4x4 invView;
uniform sampler2DShadow samplerShadow;
uniform samplerCubeShadow samplerShadowCube;
uniform mat4x4 shadowViewProj;
uniform vec2 clipPlane;
uniform float shadowBias;
uniform float texelSize;

void main()
{
	vec2 project = gl_FragCoord.xy / screenSize.xy;
	
	vec3 normal = normalize(texture2D(samplerNormal, project).xyz);
	vec3 position = texture2D(samplerPosition, project).xyz;
	
#if defined(SPOT_LIGHT) || defined(POINT_LIGHT)
	vec3 lightVec = lightPosition - position;
	float dist = length(lightVec);
	vec3 lightDir = lightVec / dist;
#else
	vec3 lightDir = -normalize(lightDirection);
	vec3 lightVec = lightDirection;
#endif
	
	vec3 eyeDir = normalize(cameraPosition - position);
	
#if defined(SPOT_LIGHT) || defined(POINT_LIGHT)
	float attenuation = saturate(1.0f - ((dist * dist) / (lightRange * lightRange)));
	attenuation = attenuation * attenuation;
	float radius = lightRange;
#else
	float attenuation = 1.0f;
	float radius = 1;
#endif
	
#ifdef SPOT_LIGHT
	float spotLightAngle = saturate(dot(-lightDirection, lightDir));
	float cosInnerMinusOuterAngle = spotParams.x - spotParams.y;
	float spotFallof = saturate((spotLightAngle - spotParams.y) / cosInnerMinusOuterAngle);
	
	attenuation *= spotFallof;
#endif

	float nDotL = saturate(dot(normal, lightDir));
	vec3 lighting = vec3(0, 0, 0);

	if (attenuation > 0 && nDotL > 0) {
#ifdef SHADOWS
#ifdef SHADOWS_CUBE
		float shadow = get_shadows_cube(samplerShadowCube, nDotL, position, invView, clipPlane, shadowBias, texelSize, lightPosition);
#else
		float shadow = get_shadows(samplerShadow, nDotL, position, invView, shadowViewProj, clipPlane, shadowBias, texelSize);
#endif
		attenuation *= shadow;
#endif
		vec3 gbuffer0 = texture2D(samplerDiffuse, project).xyz;
		vec4 gbuffer3 = texture2D(samplerSpecular, project);
		
		float metallic = gbuffer3.x;
		float roughness = gbuffer3.y;
		float specular = gbuffer3.z;
		
		vec3 diffuseColor = gbuffer0.xyz - gbuffer0.xyz * metallic;
		vec3 specularColor = mix(0.08 * vec3(specular), gbuffer0.xyz, metallic);
		
		vec3 diffuseLighting = get_diffuse(diffuseColor);
		vec3 specularLighting = get_specular(normal, eyeDir, lightVec, roughness, specularColor, lightRange);
	
		lighting = lightColor * nDotL * attenuation * (diffuseLighting + specularLighting);
	}

	oColor.xyz = lighting.xyz;
	oColor.w = 1.0f;
}
#endif