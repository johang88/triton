import(/shaders/core);
import(/shaders/post/postcommon);
#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_TEXCOORD_0) in vec2 iTexCoord;

out vec2 texCoord;

void main()
{
	texCoord = iTexCoord;
	gl_Position = vec4(iPosition, 1);
}

#else

in vec2 texCoord;

layout(location = 0) out vec4 oColor;

uniform sampler2D samplerScene;
uniform sampler2D samplerBloom;
uniform sampler2D samplerLensFlares;
uniform sampler2D samplerLuminance;
uniform float keyValue = 0.115;

vec3 uncharted2tonemap(vec3 x) {
	float A = 0.15;
	float B = 0.50;
	float C = 0.10;
	float D = 0.20;
	float E = 0.02;
	float F = 0.30;
	
	return ((x*(A*x+C*B)+D*E)/(x*(A*x+B)+D*F))-E/F;
}

vec3 reinhard(vec3 x) {
	return x / (x + 1);
}

vec3 tonemap(vec3 x) {
	return uncharted2tonemap(x);
}

void main() {
	float averageLuminance = get_average_luminance(samplerLuminance);
	vec3 color = texture2D(samplerScene, texCoord).xyz;

	color = calc_exposed_color(color, averageLuminance, 0, keyValue);
	color = tonemap(2 * color);
	
	vec3 whiteScale = vec3(1.0) / tonemap(vec3(11.2));
	color = color * whiteScale;

	vec3 bloom = texture2D(samplerBloom, texCoord).xyz;
	vec3 lensFlares = texture2D(samplerLensFlares, texCoord).xyz;
	
	oColor = vec4(color + bloom + lensFlares, 1);
}
#endif