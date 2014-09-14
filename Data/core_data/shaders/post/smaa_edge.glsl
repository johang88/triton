import(/shaders/core);
#ifdef VERTEX_SHADER

import(/shaders/post/smaa_vert);

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_TEXCOORD_0) in vec2 iTexCoord;

out vec2 texCoord;
out vec4 offset[3];

void main()
{
	texCoord = iTexCoord;
	
	vec4 dummy1 = vec4(0);
	vec4 dummy2 = vec4(0);
	
	SMAAEdgeDetectionVS(dummy1, dummy2, texCoord, offset);
	gl_Position = vec4(iPosition, 1);
}

#else

import(/shaders/post/smaa_frag);

in vec2 texCoord;
in vec4 offset[3];

layout(location = 0) out vec4 oColor;

uniform sampler2D samplerScene;

void main()
{
	oColor = SMAAColorEdgeDetectionPS(texCoord, offset, samplerScene);
}
#endif