import(/shaders/core);
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

import(/shaders/deferred/brdf);
import(/shaders/deferred/shadows);

in vec2 texCoord;

layout(location = 0) out vec4 oColor;

uniform sampler2D samplerGBuffer0;
uniform sampler2D samplerGBuffer1;
uniform sampler2D samplerGBuffer2;
uniform sampler2D samplerDepth;

uniform float lightInvSquareRadius;
uniform vec3 lightPosition;
uniform vec3 lightColor;
uniform vec2 spotParams;
uniform float lightRange;
uniform vec2 screenSize;
uniform vec3 cameraPosition;
uniform vec3 lightDirection;
uniform sampler2DShadow samplerShadow;
uniform samplerCubeShadow samplerShadowCube;
uniform mat4x4 shadowViewProj;
uniform vec2 clipPlane;
uniform float shadowBias;
uniform float texelSize;
uniform mat4x4 viewMatrix;

#ifdef DIRECTIONAL_LIGHT
uniform sampler2DShadow samplerShadow1;
uniform sampler2DShadow samplerShadow2;
uniform sampler2DShadow samplerShadow3;
uniform mat4x4 shadowViewProj1;
uniform mat4x4 shadowViewProj2;
uniform mat4x4 shadowViewProj3;
uniform vec4 clipDistances;
#endif

void main()
{
	vec2 project = gl_FragCoord.xy / screenSize.xy;
	
	vec4 gbuffer0 = texture2D(samplerGBuffer0, project); // color
	vec4 gbuffer1 = texture2D(samplerGBuffer1, project); // normal
	vec4 gbuffer2 = texture2D(samplerGBuffer2, project); // specular stuff
	
	float depth = texture2D(samplerDepth, project).x;
	vec3 position = decodeWorldPosition(project, depth);
	
#if defined(SPOT_LIGHT) || defined(POINT_LIGHT)
	vec3 lightVec = lightPosition - position;
	vec3 lightDir = normalize(lightVec);
#else
	vec3 lightDir = -normalize(lightDirection);
	vec3 lightVec = lightDir;
#endif
	
	vec3 eyeDir = normalize(cameraPosition - position);
	
#if defined(SPOT_LIGHT) || defined(POINT_LIGHT)
	float lightDistanceSquared = dot(lightVec, lightVec);
	
	float attenuation = 1.0 / lightDistanceSquared;
	attenuation = attenuation * square(saturate(1.0 - square(lightDistanceSquared * square(1.0 / lightRange))));
#else
	float attenuation = 1.0;
#endif
	
#ifdef SPOT_LIGHT
	float spotLightAngle = saturate(dot(-lightDirection, lightDir));
	float cosInnerMinusOuterAngle = spotParams.x - spotParams.y;
	float spotFallof = saturate((spotLightAngle - spotParams.y) / cosInnerMinusOuterAngle);
	
	attenuation *= spotFallof;
#endif

	vec3 normal = decodeNormals(gbuffer1.xyz);
	
	float nDotL = saturate(dot(normal, lightDir));
	vec3 lighting = vec3(0, 0, 0);

	if (attenuation > 0 && nDotL > 0) {
#ifdef SHADOWS
	#ifdef SHADOWS_CUBE
		float shadow = get_shadows_cube(samplerShadowCube, nDotL, position, clipPlane, shadowBias, texelSize, lightPosition);
	#else
		#ifdef DIRECTIONAL_LIGHT
			float shadow = 1;
			
			float n = clipDistances.x;
			float f = clipDistances.w;
			
			float linearDepth = ((2 * n) / (f + n - depth * (f - n))) * (f + n);
			
			vec3 splitColor = vec3(0, 0, 0);
			
			if (linearDepth < clipDistances.y) {
				shadow = get_shadows(samplerShadow1, nDotL, position, shadowViewProj1, texelSize);
				splitColor = vec3(1, 0, 0);
			} else if (linearDepth < clipDistances.z) {
				shadow = get_shadows(samplerShadow2, nDotL, position, shadowViewProj2, texelSize);
				splitColor = vec3(0, 1, 0);
			} else {
				shadow = get_shadows(samplerShadow3, nDotL, position, shadowViewProj3, texelSize);
				splitColor = vec3(0, 0, 1);
			}
		#else
			float shadow = get_shadows(samplerShadow, nDotL, position, shadowViewProj, texelSize);
		#endif
	#endif
		attenuation *= shadow;
#endif
		float metallic = gbuffer2.x;
		float roughness = gbuffer2.y;
		float specular = gbuffer2.z;
		
		vec3 diffuse = decodeDiffuse(gbuffer0.xyz);
		
		vec3 diffuseColor = mix(diffuse, vec3(0), metallic);
		vec3 specularColor = mix(0.08 * vec3(specular), diffuse, metallic);

		vec3 diffuseLighting = get_diffuse(diffuseColor, normal, eyeDir, lightDir, roughness);
		vec3 specularLighting = get_specular(normal, eyeDir, lightDir, roughness, specularColor);
		
		lighting = lightColor * nDotL * attenuation * (diffuseLighting + specularLighting);
	}

	oColor.xyz = lighting.xyz;
	oColor.w = 1.0;
}
#endif