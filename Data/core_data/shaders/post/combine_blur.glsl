#include "/shaders/core"
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

uniform sampler2D samplerBlur0;
uniform sampler2D samplerBlur1;
uniform sampler2D samplerBlur2;
uniform sampler2D samplerBlur3;

void main()
{
	vec3 blur0 = texture(samplerBlur0, texCoord).xyz;
	vec3 blur1 = texture(samplerBlur1, texCoord).xyz;
	vec3 blur2 = texture(samplerBlur2, texCoord).xyz;
	vec3 blur3 = texture(samplerBlur3, texCoord).xyz;
	
	vec3 res = blur0 + blur1 + blur2 + blur3;
	
	oColor.xyz = res;
	oColor.a = 1;
}
#endif