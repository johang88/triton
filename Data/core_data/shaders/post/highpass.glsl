import(/shaders/core);
import(/shaders/post/postcommon);
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
uniform sampler2D samplerLuminance;
uniform float bloomThreshold;

void main()
{
	vec3 color = texture2D(samplerScene, texCoord).xyz;
	
	float averageLuminance = get_average_luminance(samplerLuminance);

	color = calc_exposed_color(color, averageLuminance, bloomThreshold);
	
	if (dot(color, vec3(0.333)) <= 0.001)
		color = vec3(0);
	
	oColor = vec4(max(color, vec3(0)), 1.0);
}
#endif