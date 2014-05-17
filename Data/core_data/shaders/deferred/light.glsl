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
	vec4 specularData = texture2D(samplerSpecular, project);
	vec3 diffuseColor = texture2D(samplerDiffuse, project).xyz;
	vec3 position = texture2D(samplerPosition, project).xyz;
	
	vec3 specularColor = specularData.xyz;
	float specularPower = 128 * specularData.w;
	
#if defined(SPOT_LIGHT) || defined(POINT_LIGHT)
	vec3 lightDir = lightPosition - position;
	float dist = length(lightDir);
	lightDir = lightDir / dist;
#else
	vec3 lightDir = -normalize(lightDirection);
#endif
	
	vec3 eyeDir = normalize(cameraPosition - position);
	
#if defined(SPOT_LIGHT) || defined(POINT_LIGHT)
	float attenuation= saturate(1.0f - ((dist * dist) / (lightRange * lightRange)));
	attenuation = attenuation * attenuation;
#else
	float attenuation = 1.0f;
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

		float metallic = 0.1;
		float roughness = 0.6;
		
		diffuseColor = diffuseColor - diffuseColor * metallic;
		
		float baseSpec = 0.08;
		specularColor = (baseSpec - baseSpec * metallic) + diffuseColor * metallic;

		vec3 diffuseLighting = get_diffuse(diffuseColor);
		vec3 specularLighting = get_specular(normal, eyeDir, lightDir, roughness, specularColor);
	
		lighting = lightColor * nDotL * attenuation * (diffuseLighting + specularLighting);
	}

	oColor.xyz = lighting.xyz;
	oColor.w = 1.0f;
}
#endif