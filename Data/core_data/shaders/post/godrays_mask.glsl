#include "/shaders/core"
#include "/shaders/post/postcommon"
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

layout(binding = 0) uniform sampler2D samplerDepth;
layout(binding = 1) uniform sampler2D samplerScene;

void main() {
	vec3 sceneColor = texture(samplerScene, texCoord).xyz;
	float depth = texture(samplerDepth, texCoord).x;
	
	float mask = depth == 1.0 ? 1.0 : 0.0;
	oColor = vec4(sceneColor * mask, 1);
}
#endif