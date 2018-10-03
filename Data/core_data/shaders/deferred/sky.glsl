#include "/shaders/core"

#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec3 iPosition;

uniform mat4x4 modelViewProjection;
out vec3 direction;

void main()
{
	direction = position.xyz;
	direction.y = max(0, direction.y);
	
	gl_Position = modelViewProjection * vec4(iPosition, 1);
}

#else

in vec3 direction;

uniform vec3 sunDirection;
uniform vec2 thetaSun;
uniform vec3 zenithColor;
uniform float exposure;

layout(location = 0) out vec4 oColor;

void main()
{
	vec3 zenithDirection = vec3(0, 1.0, 0);
	
	vec3 A = vec3(-0.2978,-0.2941999,-1.1056);
	vec3 B = vec3(-0.1322,-0.1808,-0.2833);
	vec3 C = vec3(0.2117,0.1943999,5.2797);
	vec3 D = vec3(-1.0271,-1.7419,-2.3359);
	vec3 E = vec3(0.0385999,0.0311,0.2363);
	
	float sunBrightness = 4000.0;
	float sunSize = 0.00001;
	
	mat3x3 colorConvMat = mat3x3(vec3(3.240479,-1.53715,-0.498535),
                                     vec3(-0.969256,1.875992,0.041556),
                                     vec3(0.055648,-0.204043,1.0573109));
	
	direction = normalize(direction);
	
	float theta = dot(zenithDirection, direction);
	float gamma = dot(sunDirection, direction);
	
	float cos2gamma = gamma * gamma;
	gamma = acos(gamma);
	
	vec3 num = (1.0 + A * exp(B / theta)) * (1.0 + C * exp(D * gamma) + E * cos2gamma);
	vec3 den = (1.0 + A * exp(B)) * (1.0 + C * exp(D * thetaSun.x) + E * thetaSun.y);
	vec3 xyY = num / den * zenithColor;
	
	xyY.z = 1.0 - exp(-exposure * xyY.z);
	
	vec3 XYZ;
	XYZ.x = (xyY.x / xyY.y) * xyY.z;
	XYZ.y = xyY.z;
	XYZ.z = ((1.0 - xyY.x - xyY.y) / xyY.y) * xyY.z;
	
	float angle = exp(sin(thetaSun.x)) + cos(gamma) + 0.5;
	angle = min(1.0, angle);
	
	vec4 diffuse = vec4(XYZ * colorConvMat, 1.0f);
	float emission = sunBrightness * saturate(dot(float3(0.3086, 0.6094, 0.0820), diffuse.xyz));
	
	emission = 1.0 + lerp(0.0, emission, sunSize / (gamma + 0.0001));
	
	oColor = colorConvMat * XYZ * emission;
}
#endif