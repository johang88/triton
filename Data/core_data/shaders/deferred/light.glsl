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

uniform vec3 lightPosition;
uniform vec3 lightColor;
uniform vec2 spotParams;
uniform float lightRange;
uniform vec2 screenSize;
uniform vec3 cameraPosition;
uniform vec3 lightDirection;
uniform mat4x4 invViewProjection;
uniform sampler2DShadow samplerShadow;
uniform samplerCubeShadow samplerShadowCube;
uniform mat4x4 shadowViewProj;
uniform vec2 clipPlane;
uniform float shadowBias;
uniform float texelSize;

vec3 decodeWorldPosition(vec2 coord, float depth) {
	depth = depth * 2.0 - 1.0;
	
	vec3 clipSpacePosition = vec3(coord * 2.0 - 1.0, depth);
	vec4 worldPosition = invViewProjection * vec4(clipSpacePosition, 1);
	
	return worldPosition.xyz / worldPosition.w;
}

void main()
{
	vec2 project = gl_FragCoord.xy / screenSize.xy;
	
	vec4 gbuffer0 = texture2D(samplerGBuffer0, project); // color
	vec4 gbuffer1 = texture2D(samplerGBuffer1, project); // normal
	vec4 gbuffer2 = texture2D(samplerGBuffer2, project); // specular stuff
	
	vec3 diffuse = decodeDiffuse(gbuffer0.xyz);
	
	float depth = texture2D(samplerDepth, project).x;
	vec3 position = decodeWorldPosition(project, depth);
	
	if (gbuffer1.w == 0)
		discard;
	
	vec3 normal = decodeNormals(gbuffer1.xyz);
	
#if defined(SPOT_LIGHT) || defined(POINT_LIGHT)
	vec3 lightVec = lightPosition - position;
	float dist = length(lightVec);
	vec3 lightDir = normalize(lightVec / dist);
#else
	vec3 lightDir = -normalize(lightDirection);
	vec3 lightVec = lightDir;
#endif
	
	vec3 eyeDir = normalize(cameraPosition - position);
	
#if defined(SPOT_LIGHT) || defined(POINT_LIGHT)
	float attenuation = saturate(1.0 - ((dist * dist) / (lightRange * lightRange)));
	attenuation = attenuation * attenuation;
	float radius = lightRange;
#else
	float attenuation = 1.0;
	float radius = 1;
#endif
	
#ifdef SPOT_LIGHT
	float spotLightAngle = saturate(dot(-lightDirection, lightDir));
	float cosInnerMinusOuterAngle = spotParams.x - spotParams.y;
	float spotFallof = saturate((spotLightAngle - spotParams.y) / cosInnerMinusOuterAngle);
	
	attenuation *= spotFallof;
#endif

	float nDotL = saturate(dot(normal, lightDir) * 1.08 - 0.08);
	vec3 lighting = vec3(0, 0, 0);

	if (attenuation > 0 && nDotL > 0) {
#ifdef SHADOWS
#ifdef SHADOWS_CUBE
		float shadow = get_shadows_cube(samplerShadowCube, nDotL, position, clipPlane, shadowBias, texelSize, lightPosition);
#else
		float shadow = get_shadows(samplerShadow, nDotL, position, shadowViewProj, clipPlane, shadowBias, texelSize);
#endif
		attenuation *= shadow;
#endif
		float metallic = gbuffer2.x;
		float roughness = gbuffer2.y;
		float specular = gbuffer2.z;
		
		vec3 diffuseColor = mix(diffuse, vec3(0), metallic);
		vec3 specularColor = mix(0.08 * vec3(specular), diffuse, metallic);

		vec3 diffuseLighting = get_diffuse(diffuseColor, normal, eyeDir, lightDir, roughness);
		vec3 specularLighting = get_specular(normal, eyeDir, lightDir, roughness, specularColor, lightRange);
		
		lighting = lightColor * nDotL * attenuation * (diffuseLighting + specularLighting);
	}

	oColor.xyz = lighting.xyz;
	oColor.w = 1.0;
}
#endif