#include "/shaders/core"
#include "/shaders/brdf"

#define COMPUTE

#define LightTileSize 16
#define MaxLights 2048

layout(local_size_x = LightTileSize, local_size_y = LightTileSize) in;

struct Light {
	vec4 positionRange;
	vec4 colorIntensity;
};

layout(std430, binding = 0) buffer Lights {
	Light lights[];
};

uniform mat4x4 view;
uniform mat4x4 projection;
uniform vec3 cameraPositionWS;
uniform vec2 cameraClipPlanes;
uniform uvec2 numTiles;
uniform uvec2 displaySize;
uniform int numLights;

layout(binding = 0) uniform sampler2D samplerDepth;
layout(binding = 0, rgba8) uniform image2D samplerGBuffer0;
layout(binding = 1, rgba16f) uniform image2D samplerGBuffer1;
layout(binding = 2, rgba8) uniform image2D samplerGBuffer2;
layout(binding = 3, rgba16f) uniform image2D outputTexture;

shared uint tileMinZ;
shared uint tileMaxZ;
shared uint tileLightList[MaxLights];
shared uint numTileLights;

float linearDepth(float zw) {
	float n = cameraClipPlanes.x;
	float f = cameraClipPlanes.y;

	zw = 2.0 * zw - 1.0;
	return (2 * n * f) / (f + n - zw * (f - n));
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

void main() {
	// Initialize shared memory
	if (gl_LocalInvocationIndex == 0) {
		numTileLights = 0;
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

	// Check lights against the frustum
	for (uint i = 0; i < numLights; i += ThreadGroupSize) {
		uint lightIndex = gl_LocalInvocationIndex + i;
		if (lightIndex < numLights) {
			vec4 lightPositionVS = view * vec4(lights[lightIndex].positionRange.xyz, 1.0);
			float radius = lights[lightIndex].positionRange.w;

			if (   get_signed_distance_from_plane(lightPositionVS, frustum[0]) < radius
				&& get_signed_distance_from_plane(lightPositionVS, frustum[1]) < radius
				&& get_signed_distance_from_plane(lightPositionVS, frustum[2]) < radius
				&& get_signed_distance_from_plane(lightPositionVS, frustum[3]) < radius) {
					uint listIndex = atomicAdd(numTileLights, 1);
            		tileLightList[listIndex] = lightIndex;
				}
		}
	}

	// Synchronize
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
		for(uint tLightIdx = 0; tLightIdx < numTileLights; ++tLightIdx) {
			uint lIdx = tileLightList[tLightIdx];
			Light light = lights[lIdx];
			
			vec3 lightVec = light.positionRange.xyz - positionWS;
			vec3 lightDir = normalize(lightVec);
			
			float nDotL = saturate(dot(normalWS, lightDir));
			
			float lightDistanceSquared = dot(lightVec, lightVec);
		
			float attenuation = 1.0 / lightDistanceSquared;
			attenuation = attenuation * square(saturate(1.0 - square(lightDistanceSquared * square(1.0 / light.positionRange.w))));

			float attenuationNdotL = attenuation * nDotL;
			
			lighting += brdf(normalWS, eyeDir, lightDir, roughness, metallic, light.colorIntensity.xyz * attenuationNdotL, diffuse, F0);
			//lighting.r += 0.01;
		}
		
		imageStore(outputTexture, pixelCoord, vec4(lighting, 0));
	}
}