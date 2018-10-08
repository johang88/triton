uniform int tonemapOperator = 0;
uniform float linearWhite = 11.2;
uniform float keyValue = 0.115;
uniform int autoKey = 1;

// Average luminance
float calc_luminance(vec3 color) {
	return max(dot(color, vec3(0.299, 0.587, 0.114)), 0.0001);
}

float get_average_luminance(sampler2D samplerAverageLuminance) {
	return texelFetch(samplerAverageLuminance, ivec2(0, 0), 0).x;
}

vec3 calc_exposed_color(vec3 color, float averageLuminance, float threshold) {
	averageLuminance = max(averageLuminance, 0.001);
	
	float key = keyValue;
	if (autoKey == 1) {
		key = 1.03 - (2.0 / (2.0 + (log(averageLuminance + 1) / log(exp(1)))));
	}

	float linearExposure = key / averageLuminance;
	float exposure = log2(max(linearExposure, 0.0001));
	
	exposure -= threshold;
	return color * exp2(exposure);
}

// Tone mapping
vec3 uncharted2_tonemap(vec3 x) {
	float A = 0.15;
	float B = 0.50;
	float C = 0.10;
	float D = 0.20;
	float E = 0.02;
	float F = 0.30;
	
	return ((x*(A*x+C*B)+D*E)/(x*(A*x+B)+D*F))-E/F;
}

vec3 uncharted2_2tonemap(vec3 x) {
	float a = 10.0; /// Mid
	float b = 0.3; /// Toe
	float c = 0.5; /// Shoulder
	float d = 1.5; /// Mid

	return (x * (a * x + b)) / (x * (a * x + c) + d);
}

vec3 uncharted2_3tonemap(vec3 x) {
	float a = 1.8; /// Mid
	float b = 1.4; /// Toe
	float c = 0.5; /// Shoulder
	float d = 1.5; /// Mid

	// return (x * (a * x + b)) * rcp(x * (a * x + c) + d);
	return x;
}

vec3 reinhard(vec3 x) {
	return x / (x + 1);
}

vec3 filmicalu(vec3 c) {
	c = max(vec3(0), c - 0.004);
	c = (c* (6.2 * c + 0.5)) / (c * (6.2 * c + 1.7) + 0.06);
	
	// 1/2.2 built in, need to go back to linear
	return pow(c, vec3(2.2));
}

vec3 asecFilm(vec3 x) {
    float a = 2.51f;
    float b = 0.03f;
    float c = 2.43f;
    float d = 0.59f;
    float e = 0.14f;
    return min(vec3(1.0), max(vec3(0.0), (x*(a*x+b))/(x*(c*x+d)+e)));
}

#define TONEMAP_REINHARD 0
#define TONEMAP_UNCHARTED 1
#define TONEMAP_UNCHARTED_2 2
#define TONEMAP_UNCHARTED_3 3
#define TONEMAP_FILMICALU 4
#define TONEMAP_ASEC 5

vec3 tonemap(vec3 color, float averageLuminance, float threshold) {
	color = calc_exposed_color(color, averageLuminance, threshold);
	
	switch (tonemapOperator) {
		case TONEMAP_REINHARD:
			return reinhard(color);
		case TONEMAP_UNCHARTED:
			return uncharted2_tonemap(color) / uncharted2_tonemap(vec3(linearWhite));
		case TONEMAP_UNCHARTED_2:
			return uncharted2_2tonemap(color) / uncharted2_2tonemap(vec3(linearWhite));
		case TONEMAP_UNCHARTED_3:
			return uncharted2_3tonemap(color) / uncharted2_3tonemap(vec3(linearWhite));
		case TONEMAP_FILMICALU:
			return filmicalu(color);
		case TONEMAP_ASEC:
			return asecFilm(color);
		default:
			return filmicalu(color);
	}
}