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

void main() {
	ivec2 pixelCoord = ivec2(gl_WorkGroupID.xy * uvec2(LightTileSize, LightTileSize) + gl_LocalInvocationID.xy);	
	const uint groupThreadIdx = gl_LocalInvocationID.y * LightTileSize + gl_LocalInvocationID.x;
	
	// Work out z range
	float minZSample = cameraClipPlanes.x;
	float maxZSample = cameraClipPlanes.y;
	
	vec2 depthTextureCoords = vec2(pixelCoord) / displaySize;
	float zw = texelFetch(samplerDepth, pixelCoord, 0).x;
	
	float linearZ = linearDepth(zw);
	vec3 positionWS = decodeWorldPosition(depthTextureCoords, zw);
	
	minZSample = min(minZSample, linearZ);
	maxZSample = max(maxZSample, linearZ);
	
	// Initialize shared memory
	if (groupThreadIdx == 0) {
		numTileLights = 0;
		
		tileMinZ = 0xffffffff;
		tileMaxZ = 0;
	}
	
	groupMemoryBarrier(); barrier();
	
	if (maxZSample >= minZSample) {
		atomicMin(tileMinZ, uint(minZSample));
		atomicMax(tileMaxZ, uint(maxZSample));
	}
	
	groupMemoryBarrier();  barrier();
	
	float minTileZ = float(tileMinZ);
    float maxTileZ = float(tileMaxZ);
	
	// Work out scale/bias from [0, 1]
	vec2 tileScale = vec2(displaySize.xy) * (1.0 / (2.0 * float(LightTileSize)));
	vec2 tileBias = tileScale - vec2(gl_WorkGroupID.xy);
	
	// Now work out composite projection matrix
    // Relevant matrix columns for this tile frusta
    vec4 c1 = vec4(-projection[0][0] * tileScale.x, projection[0][1], tileBias.x, projection[0][3]);
	vec4 c2 = vec4(projection[1][0], -projection[1][1] * tileScale.y, tileBias.y, projection[1][3]);
    vec4 c4 = vec4(projection[3][0], projection[3][1], -1.0, projection[3][3]);
	
	// Derive frustum planes
	vec4 frustumPlanes[6];
	
	 // Sides
    frustumPlanes[0] = c4 - c1;
    frustumPlanes[1] = c4 + c1;
    frustumPlanes[2] = c4 - c2;
    frustumPlanes[3] = c4 + c2;
	
	// Near/far
	frustumPlanes[4] = vec4(0.0, 0.0, -1.0, -minTileZ);
	frustumPlanes[5] = vec4(0.0, 0.0, -1.0, maxTileZ);
	
	for (uint i = 0; i < 4; ++i) {
		frustumPlanes[i] *= 1.0 / length(frustumPlanes[i].xyz);
	}
	
	// Cull lights
	for(uint lightIndex = groupThreadIdx; lightIndex < numLights; lightIndex += ThreadGroupSize) {
		vec3 lightPosition = (view * vec4(lights[lightIndex].positionRange.xyz, 1.0)).xyz;
		float cutoffRadius = lights[lightIndex].positionRange.w;

        // Cull: point light sphere vs tile frustum
        bool inFrustum = true;
		for (uint i = 0; i < 4; ++i) {
            float d = dot(frustumPlanes[i], vec4(lightPosition, 1.0));
            inFrustum = inFrustum && (-cutoffRadius <= d);
        }
		
		if (inFrustum) {
            uint listIndex = atomicAdd(numTileLights, 1);
            tileLightList[listIndex] = lightIndex;
        }
    }

    groupMemoryBarrier();  barrier();
	
	vec3 diffuse = decodeDiffuse(imageLoad(samplerGBuffer0, pixelCoord).xyz);
	vec3 normalWS = decodeNormals(imageLoad(samplerGBuffer1, pixelCoord).xyz);
	vec4 gbuffer2 = imageLoad(samplerGBuffer2, pixelCoord);
	
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
	}
	
	imageStore(outputTexture, pixelCoord, vec4(lighting, 0));
}