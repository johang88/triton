import(shaders/core);
import(shaders/post/postcommon);
#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_TEXCOORD_0) in vec2 iTexCoord;

out vec2 texCoord;

uniform mat4x4 modelViewProjection;

void main()
{
	texCoord = iTexCoord;
	gl_Position = modelViewProjection * vec4(iPosition, 1);
}

#else

in vec2 texCoord;

layout(location = 0) out vec4 oColor;

uniform sampler2D samplerScene;
uniform sampler2D samplerBloom;
uniform sampler2D samplerLuminance;

vec3 tonemap(vec3 x)
{
	float A = 0.15;
	float B = 0.50;
	float C = 0.10;
	float D = 0.20;
	float E = 0.02;
	float F = 0.30;
	
	return ((x*(A*x+C*B)+D*E)/(x*(A*x+B)+D*F))-E/F;
}

void main()
{
	float averageLuminance = get_average_luminance(samplerLuminance);
	vec3 color = texture2D(samplerScene, texCoord).xyz;

	color = calc_exposed_color(color, averageLuminance, 0);
	color = tonemap(color);

	vec3 bloom = texture2D(samplerBloom, texCoord).xyz;
	
	vec3 final = color + bloom;
	final = pow(final, vec3(1.0 / 2.2));
	oColor = vec4(final, 1);
}
#endif