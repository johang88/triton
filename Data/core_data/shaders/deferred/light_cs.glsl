#include "/shaders/core"
#include "/shaders/brdf"
#include "/shaders/deferred/shadows"

#define COMPUTE

#define LightTileSize 16
#define MaxLights 1024

layout(local_size_x = LightTileSize, local_size_y = LightTileSize) in;

#define SpotLightShadowIndexOffset 1024
#define MaxShadowCastingSpotLights 8

#define PointLightShadowIndexOffset 0
#define MaxShadowCastingPointLights 6

struct PointLight {
	vec4 positionRange;
	vec4 colorIntensity;
};

struct SpotLight {
	vec4 positionRange;
	vec4 colorInnerAngle;
	vec4 directionOuterAngle;
};

layout(std430, binding = 0) buffer PointLights {
	PointLight pointLights[];
};

layout(std430, binding = 1) buffer SpotLights {
	SpotLight spotLights[];
};

layout(std430, binding = 2) buffer ShadowMapIndexes {
	int lightToShadowIndex[];
};

uniform mat4x4 view;
uniform mat4x4 projection;
uniform vec3 cameraPositionWS;
uniform vec2 cameraClipPlanes;
uniform uvec2 numTiles;
uniform uvec2 displaySize;
uniform int numPointLights;
uniform int numSpotLights;
uniform mat4x4[MaxShadowCastingSpotLights] spotShadowMatrices;
uniform mat4x4[MaxShadowCastingPointLights * 6] pointShadowMatrices;

layout(binding = 0) uniform sampler2D samplerDepth;
layout(binding = 1) uniform sampler2D samplerSpotLightShadowAtlas;
layout(binding = 2) uniform sampler2D samplerPointLightShadowAtlas;

layout(binding = 0, rgba8) uniform image2D samplerGBuffer0;
layout(binding = 1, rgba16f) uniform image2D samplerGBuffer1;
layout(binding = 2, rgba8) uniform image2D samplerGBuffer2;
layout(binding = 3, rgba16f) uniform image2D outputTexture;

shared uint tileMinZ;
shared uint tileMaxZ;
shared uint tilePointLightList[MaxLights];
shared uint numTilePointLights;
shared uint tileSpotLightList[MaxLights];
shared uint numTileSpotLights;

float linearDepth(float zw) {
	float n = cameraClipPlanes.x;
	float f = cameraClipPlanes.y;

	return (2.0 * n) / (f + n - zw * (f - n));
}

const uint ThreadGroupSize = LightTileSize * LightTileSize;

vec4 unproject(vec4 v) {
	v = invProjection * v;
	v /= v.w;
	return v;
}

vec4 create_plane(vec4 b, vec4 c) { 
    vec4 normal = vec4(normalize(cross(b.xyz, c.xyz)), 0);
    return normal;
}

float get_signed_distance_from_plane(vec4 p, vec4 eqn) {
	return dot(eqn.xyz, p.xyz);
}

int vec3_to_face(vec3 v) {
	int face = 0;
	float x = v.x;
	float y = v.y;
	float z = v.z;

	if ((abs(x) > abs(y)) && (abs(x) > abs(z))) {
		if (x >= 0) {
			face = 0;
		} else {
			face = 1;
		}
	} else if ((abs(y) > abs(x)) && (abs(y) > abs(z))) {
		if (y >= 0) {
			face = 2;
		} else {
			face = 3;
		}
	} else {
		if (z >= 0) {
			face = 4;
		} else {
			face = 5;
		}
	}

	return face;
}

void main() {
	// Initialize shared memory
	if (gl_LocalInvocationIndex == 0) {
		numTilePointLights = 0;
		numTileSpotLights = 0;
		tileMinZ = 0xffffffff;
		tileMaxZ = 0;
	}

	ivec2 pixelCoord = ivec2(gl_GlobalInvocationID.xy);	
	vec2 texCoord = vec2(pixelCoord) / displaySize;

	float depthFloat = texelFetch(samplerDepth, pixelCoord, 0).x;
	uint depthInt = uint(depthFloat * 0xffffffffu);

	// Calculate min / max depth
	atomicMin(tileMinZ, depthInt);
	atomicMax(tileMaxZ, depthInt);

	// Synchronize
	groupMemoryBarrier(); barrier();

	float maxDepthZ = float(float(tileMaxZ) / float(0xffffffffu));
	float minDepthZ = float(tileMinZ / float(0xffffffffu));

	// Create frustum tiles for the lights
	uint minX = LightTileSize * gl_WorkGroupID.x;
	uint minY = LightTileSize * gl_WorkGroupID.y;
	uint maxX = LightTileSize * (gl_WorkGroupID.x + 1);
	uint maxY = LightTileSize * (gl_WorkGroupID.y + 1);

	// Convert to NDC and then viewspace
	vec4 tileCorners[4];
	tileCorners[0] = unproject(vec4( (float(minX)/displaySize.x) * 2.0f - 1.0f, (float(minY)/displaySize.y) * 2.0f - 1.0f, 1.0f, 1.0f));
	tileCorners[1] = unproject(vec4( (float(maxX)/displaySize.x) * 2.0f - 1.0f, (float(minY)/displaySize.y) * 2.0f - 1.0f, 1.0f, 1.0f));
	tileCorners[2] = unproject(vec4( (float(maxX)/displaySize.x) * 2.0f - 1.0f, (float(maxY)/displaySize.y) * 2.0f - 1.0f, 1.0f, 1.0f));
	tileCorners[3] = unproject(vec4( (float(minX)/displaySize.x) * 2.0f - 1.0f, (float(maxY)/displaySize.y) * 2.0f - 1.0f, 1.0f, 1.0f));

	vec4 frustum[4];
	for(int i = 0; i < 4; i++) {
		frustum[i] = create_plane(tileCorners[i], tileCorners[(i+1) & 3]);
	}

	// Synchronize
	groupMemoryBarrier(); barrier();

	// Check lights against the frustum (point)
	for (uint i = 0; i < numPointLights; i += ThreadGroupSize) {
		uint lightIndex = gl_LocalInvocationIndex + i;
		if (lightIndex < numPointLights) {
			vec4 lightPositionVS = view * vec4(pointLights[lightIndex].positionRange.xyz, 1.0);
			float radius = pointLights[lightIndex].positionRange.w;

			if (   get_signed_distance_from_plane(lightPositionVS, frustum[0]) < radius
				&& get_signed_distance_from_plane(lightPositionVS, frustum[1]) < radius
				&& get_signed_distance_from_plane(lightPositionVS, frustum[2]) < radius
				&& get_signed_distance_from_plane(lightPositionVS, frustum[3]) < radius) {
					uint listIndex = atomicAdd(numTilePointLights, 1);
            		tilePointLightList[listIndex] = lightIndex;
				}
		}
	}

	// Synchronize
	groupMemoryBarrier(); barrier();

	// Check lights against the frustum (spot)
	for (uint i = 0; i < numSpotLights; i += ThreadGroupSize) {
		uint lightIndex = gl_LocalInvocationIndex + i;
		if (lightIndex < numSpotLights) {
			vec4 lightPositionVS = view * vec4(spotLights[lightIndex].positionRange.xyz, 1.0);
			float radius = spotLights[lightIndex].positionRange.w;

			if (   get_signed_distance_from_plane(lightPositionVS, frustum[0]) < radius
				&& get_signed_distance_from_plane(lightPositionVS, frustum[1]) < radius
				&& get_signed_distance_from_plane(lightPositionVS, frustum[2]) < radius
				&& get_signed_distance_from_plane(lightPositionVS, frustum[3]) < radius) {
					uint listIndex = atomicAdd(numTileSpotLights, 1);
            		tileSpotLightList[listIndex] = lightIndex;
				}
		}
	}

	groupMemoryBarrier(); barrier();

	// Process lighting
	vec3 positionWS = decodeWorldPosition(texCoord, depthFloat);
	
	vec4 gbuffer1 = imageLoad(samplerGBuffer1, pixelCoord);
	vec3 diffuse = decodeDiffuse(imageLoad(samplerGBuffer0, pixelCoord).xyz);
	vec3 normalWS = decodeNormals(gbuffer1.xyz);
	vec4 gbuffer2 = imageLoad(samplerGBuffer2, pixelCoord);
	
	if (gbuffer1.w > 0) {
		float metallic = gbuffer2.x;
		float roughness = gbuffer2.y;
		float specular = gbuffer2.z;

		vec3 F0 = vec3(0.08);
		F0 = mix(F0, diffuse, metallic);

		vec3 eyeDir = normalize(cameraPositionWS - positionWS);
		
		vec3 lighting = imageLoad(outputTexture, pixelCoord).xyz;

		// Point lights
		for(uint tLightIdx = 0; tLightIdx < numTilePointLights; ++tLightIdx) {
			uint lIdx = tilePointLightList[tLightIdx];
			PointLight light = pointLights[lIdx];
			
			vec3 lightVec = light.positionRange.xyz - positionWS;
			vec3 lightDir = normalize(lightVec);
			
			float nDotL = saturate(dot(normalWS, lightDir));
			
			float lightDistanceSquared = dot(lightVec, lightVec);
		
			float attenuation = 1.0 / lightDistanceSquared;
			attenuation = attenuation * square(saturate(1.0 - square(lightDistanceSquared * square(1.0 / light.positionRange.w))));

			float attenuationNdotL = attenuation * nDotL;

			// Shadows!
			int shadowMapIndex = lightToShadowIndex[PointLightShadowIndexOffset + lIdx];
			if (shadowMapIndex > -1) {
				vec3 luv = -lightDir;
				luv.z = -luv.z;

				int face = vec3_to_face(luv);

				// Sample shadows!
				vec4 shadowUv = pointShadowMatrices[shadowMapIndex * 6 + face] * vec4(positionWS, 1.0);
				shadowUv.xyz = 0.5 * shadowUv.xyz / shadowUv.w + vec3(0.5);

				shadowUv.x += face;
				shadowUv.x *= 1.0 / float(6);

				shadowUv.y += shadowMapIndex;
				shadowUv.y *= 1.0 / float(MaxShadowCastingPointLights);

				float distance = shadowUv.z;
				vec2 uv = shadowUv.xy;

				#if SHADOW_QUALITY == 3
					float shadow = sample_shadow_29(samplerPointLightShadowAtlas, uv, distance);
				#elif SHADOW_QUALITY == 2
					float shadow = sample_shadow_12(samplerPointLightShadowAtlas, uv, distance);
				#elif SHADOW_QUALITY == 1
					float shadow = sample_shadow_5(samplerPointLightShadowAtlas, uv, distance);
				#else 
					float shadow = check_shadow_map(samplerPointLightShadowAtlas, uv, distance);
				#endif

				attenuationNdotL *= shadow;
			}
			
			lighting += brdf(normalWS, eyeDir, lightDir, roughness, metallic, light.colorIntensity.xyz * attenuationNdotL, diffuse, F0);
		}
		
		// Spot lights
		for(uint tLightIdx = 0; tLightIdx < numTileSpotLights; ++tLightIdx) {
			uint lIdx = tileSpotLightList[tLightIdx];
			SpotLight light = spotLights[lIdx];
			
			vec3 lightVec = light.positionRange.xyz - positionWS;
			vec3 lightDir = normalize(lightVec);
			
			float nDotL = saturate(dot(normalWS, lightDir));
			
			float lightDistanceSquared = dot(lightVec, lightVec);
		
			float attenuation = 1.0 / lightDistanceSquared;
			attenuation = attenuation * square(saturate(1.0 - square(lightDistanceSquared * square(1.0 / light.positionRange.w))));

			float innerAngle = light.colorInnerAngle.w;
			float outerAngle = light.directionOuterAngle.w;

			float spotLightAngle = saturate(dot(-light.directionOuterAngle.xyz, lightDir));
			float cosInnerMinusOuterAngle = innerAngle - outerAngle;
			float spotFallof = 1.0 - saturate((spotLightAngle - outerAngle) / cosInnerMinusOuterAngle);
			
			attenuation *= spotFallof;

			float attenuationNdotL = attenuation * nDotL;

			// Shadows!
			int shadowMapIndex = lightToShadowIndex[SpotLightShadowIndexOffset + lIdx];
			if (shadowMapIndex > -1) {
				// Sample shadows!
				vec4 shadowUv = spotShadowMatrices[shadowMapIndex] * vec4(positionWS, 1.0);
				shadowUv.xyz = 0.5 * shadowUv.xyz / shadowUv.w + vec3(0.5);

				shadowUv.x += shadowMapIndex;
				shadowUv.x *= 1.0 / float(MaxShadowCastingSpotLights);

				float distance = shadowUv.z;
				vec2 uv = shadowUv.xy;

				#if SHADOW_QUALITY == 3
					float shadow = sample_shadow_29(samplerSpotLightShadowAtlas, uv, distance);
				#elif SHADOW_QUALITY == 2
					float shadow = sample_shadow_12(samplerSpotLightShadowAtlas, uv, distance);
				#elif SHADOW_QUALITY == 1
					float shadow = sample_shadow_5(samplerSpotLightShadowAtlas, uv, distance);
				#else 
					float shadow = check_shadow_map(samplerSpotLightShadowAtlas, uv, distance);
				#endif

				attenuationNdotL *= shadow;
			}
			
			lighting += brdf(normalWS, eyeDir, lightDir, roughness, metallic, light.colorInnerAngle.xyz * attenuationNdotL, diffuse, F0);
		}

		imageStore(outputTexture, pixelCoord, vec4(lighting, 0));
	}
}