#define SHADOW_QUALITY_LOW 0
#define SHADOW_QUALITY_MEDIUM 1
#define SHADOW_QUALITY_HIGH 2

#ifndef SHADOW_QUAILTY
#define SHADOW_QUALITY 2
#endif

vec2 poisson_disc_samples_5[5] = vec2[](
	vec2(0.000000, 2.500000),
	vec2(2.377641, 0.772542),
	vec2(1.469463, -2.022543),
	vec2(-1.469463, -2.022542),
	vec2(-2.377641, 0.772543)
);

vec2 poisson_disc_samples_12[12] = vec2[](
	vec2(0.000000, 2.500000),
	vec2(1.767767, 1.767767),
	vec2(2.500000, -0.000000),
	vec2(1.767767, -1.767767),
	vec2(-0.000000, -2.500000),
	vec2(-1.767767, -1.767767),
	vec2(-2.500000, 0.000000),
	vec2(-1.767766, 1.767768),
	vec2(-1.006119, -0.396207),
	vec2(1.000015, 0.427335),
	vec2(0.416807, -1.006577),
	vec2(-0.408872, 1.024430)
);

vec2 poisson_disc_samples_29[29] = vec2[](
	vec2(0.000000, 2.500000),
	vec2(1.016842, 2.283864),
	vec2(1.857862, 1.672826),
	vec2(2.377641, 0.772542),
	vec2(2.486305, -0.261321),
	vec2(2.165063, -1.250000),
	vec2(1.469463, -2.022543),
	vec2(0.519779, -2.445369),
	vec2(-0.519779, -2.445369),
	vec2(-1.469463, -2.022542),
	vec2(-2.165064, -1.250000),
	vec2(-2.486305, -0.261321),
	vec2(-2.377641, 0.772543),
	vec2(-1.857862, 1.672827),
	vec2(-1.016841, 2.283864),
	vec2(0.091021, -0.642186),
	vec2(0.698035, 0.100940),
	vec2(0.959731, -1.169393),
	vec2(-1.053880, 1.180380),
	vec2(-1.479156, -0.606937),
	vec2(-0.839488, -1.320002),
	vec2(1.438566, 0.705359),
	vec2(0.067064, -1.605197),
	vec2(0.728706, 1.344722),
	vec2(1.521424, -0.380184),
	vec2(-0.199515, 1.590091),
	vec2(-1.524323, 0.364010),
	vec2(-0.692694, -0.086749),
	vec2(-0.082476, 0.654088)
);

float random(vec3 seed, int i){
	vec4 seed4 = vec4(seed,i);
	float dot_product = dot(seed4, vec4(12.9898, 78.233, 45.164, 94.673));
	return fract(sin(dot_product) * 43758.5453);
}

float sample_shadow_5(sampler2DShadow shadowMap, vec2 uv, vec3 worldPos, float texelSize, float distance, float shadowBias) {
	float result = 0.0f;
	for (int i = 0; i < 5; i++) {
		int index = int(16.0 * random(floor(worldPos.xyz * 1000.0f), i)) % 16;
		result += texture(shadowMap, vec3(uv.xy - poisson_disc_samples_5[i] * texelSize, distance - shadowBias * length(poisson_disc_samples_29[i])));
	}
	
	return result / 5;
}

float sample_shadow_12(sampler2DShadow shadowMap, vec2 uv, vec3 worldPos, float texelSize, float distance, float shadowBias) {
	float result = 0.0f;
	for (int i = 0; i < 12; i++) {
		int index = int(16.0 * random(floor(worldPos.xyz * 1000.0f), i)) % 16;
		result += texture(shadowMap, vec3(uv.xy - poisson_disc_samples_12[i] * texelSize, distance - shadowBias * length(poisson_disc_samples_29[i])));
	}
	
	return result / 12;
}

float sample_shadow_29(sampler2DShadow shadowMap, vec2 uv, vec3 worldPos, float texelSize, float distance, float shadowBias) {
	float result = 0.0f;
	for (int i = 0; i < 29; i++) {
		int index = int(16.0 * random(floor(worldPos.xyz * 1000.0f), i)) % 16;
		result += texture(shadowMap, vec3(uv.xy - poisson_disc_samples_29[i] * texelSize, distance - shadowBias * length(poisson_disc_samples_29[i])));
	}
	
	return result / 29;
}

float check_shadow(sampler2DShadow shadowMap, vec3 viewPos, mat4x4 invView, mat4x4 shadowViewProj, vec2 clipPlane, float shadowBias, float texelSize) {
	vec3 worldPos = (invView * vec4(viewPos, 1)).xyz;
	vec4 shadowUv = shadowViewProj * vec4(worldPos, 1);

	vec2 uv = 0.5f * shadowUv.xy / shadowUv.w + vec2(0.5f, 0.5f);
	
	float distance = (shadowUv.z / (clipPlane.y - clipPlane.x));
	
#if SHADOW_QUALITY == SHADOW_QUALITY_HIGH
	return sample_shadow_29(shadowMap, uv, worldPos, texelSize, distance, shadowBias);
#elif SHADOW_QUALITY == SHADOW_QUALITY_MEDIUM
	return sample_shadow_12(shadowMap, uv, worldPos, texelSize, distance, shadowBias);
#else
	return sample_shadow_5(shadowMap, uv, worldPos, texelSize, distance, shadowBias);
#endif
}

float check_shadow_cube(samplerCubeShadow shadowMap, vec3 viewPos, mat4x4 invView, mat4x4 shadowView, mat4x4 shadowProj, vec2 clipPlane, float shadowBias) {
	vec3 worldPos = (invView * vec4(viewPos, 1)).xyz;
	
	vec4 position_ls = shadowView * vec4(worldPos, 1);
	vec4 abs_position = abs(position_ls);
	float fs_z = -max(abs_position.x, max(abs_position.y, abs_position.z));
	
	vec4 shadowUv = shadowProj * vec4(0, 0, fs_z, 0);
	
	float distance = (shadowUv.z / (clipPlane.y - clipPlane.x)) - shadowBias;

	return texture(shadowMap, vec4(position_ls.xyz, distance));
}

float get_shadows(sampler2DShadow shadowMap, float nDotL, vec3 viewPos, mat4x4 invView, mat4x4 shadowViewProj, vec2 clipPlane, float shadowBias, float texelSize) {
	if (nDotL <= 0)
		return 0;
		
	float bias = shadowBias;
	
	bias = shadowBias * tan(acos(nDotL));
	bias = clamp(bias, 0.0f, shadowBias * 2.0f);
	
	return check_shadow(shadowMap, viewPos, invView, shadowViewProj, clipPlane, bias, texelSize);
}