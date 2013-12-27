const int SAMPLES = 4;

vec2 poissonDisk[16] = vec2[](
	vec2( -0.94201624, -0.39906216 ),
	vec2( 0.94558609, -0.76890725 ),
	vec2( -0.094184101, -0.92938870 ),
	vec2( 0.34495938, 0.29387760 ),
	vec2( -0.91588581, 0.45771432 ),
	vec2( -0.81544232, -0.87912464 ),
	vec2( -0.38277543, 0.27676845 ),
	vec2( 0.97484398, 0.75648379 ),
	vec2( 0.44323325, -0.97511554 ),
	vec2( 0.53742981, -0.47373420 ),
	vec2( -0.26496911, -0.41893023 ),
	vec2( 0.79197514, 0.19090188 ),
	vec2( -0.24188840, 0.99706507 ),
	vec2( -0.81409955, 0.91437590 ),
	vec2( 0.19984126, 0.78641367 ),
	vec2( 0.14383161, -0.14100790 )
);

float random(vec3 seed, int i){
	vec4 seed4 = vec4(seed,i);
	float dot_product = dot(seed4, vec4(12.9898, 78.233, 45.164, 94.673));
	return fract(sin(dot_product) * 43758.5453);
}

float check_shadow(sampler2DShadow shadowMap, vec3 viewPos, mat4x4 invView, mat4x4 shadowViewProj, vec2 clipPlane, float shadowBias, float texelSize) {
	vec3 worldPos = (invView * vec4(viewPos, 1)).xyz;
	vec4 shadowUv = shadowViewProj * vec4(worldPos, 1);

	vec2 uv = 0.5f * shadowUv.xy / shadowUv.w + vec2(0.5f, 0.5f);
	
	float distance = (shadowUv.z / (clipPlane.y - clipPlane.x)) - shadowBias;
	
	float c = 0.0f;
	for (int i = 0; i < SAMPLES; i++) {
		int index = int(16.0 * random(floor(worldPos.xyz * 1000.0f), i)) % 16;
		c += texture(shadowMap, vec3(uv.xy - poissonDisk[index] * texelSize, distance));
	}
	
	return c / SAMPLES;
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