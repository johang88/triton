

// calculate average luminance
float calc_luminance(vec3 color) {
	return max(dot(color, vec3(0.299, 0.587, 0.114)), 0.00001);
}

float get_average_luminance(sampler2D samplerAverageLuminance) {
	return texelFetch(samplerAverageLuminance, ivec2(0, 0), 0).x;
}

vec3 calc_exposed_color(vec3 color, float averageLuminance, float threshold, float keyValue) {
	averageLuminance = max(averageLuminance, 0.001);
	
	float linearExposure = keyValue / averageLuminance;
	
	float exposure = log2(max(linearExposure, 0.0001));
	exposure -= threshold;
	return color * exp2(exposure);
}