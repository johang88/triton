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

void main()
{
	vec3 color = texture(samplerScene, texCoord).xyz;
	float luminance = log(max(calc_luminance(color), 0.00001));
	oColor = vec4(luminance, 1, 1, 1);
}
#endif