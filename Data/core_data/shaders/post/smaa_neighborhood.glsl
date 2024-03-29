#include "/shaders/core"
#ifdef VERTEX_SHADER

#include "/shaders/post/smaa_vert"

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_TEXCOORD_0) in vec2 iTexCoord;

out vec2 texCoord;
out vec4 offset[2];

void main()
{
	texCoord = iTexCoord;
	
	vec4 dummy1 = vec4(0);
	vec4 dummy2 = vec4(0);
	
	SMAANeighborhoodBlendingVS(dummy1, dummy2, texCoord, offset);
	gl_Position = vec4(iPosition, 1);
}

#else

#include "/shaders/post/smaa_frag"

in vec2 texCoord;
in vec4 offset[2];

layout(location = 0) out vec4 oColor;

uniform sampler2D samplerScene;
uniform sampler2D samplerBlend;

void main()
{
	oColor = SMAANeighborhoodBlendingPS(texCoord, offset, samplerScene, samplerBlend);
}
#endif