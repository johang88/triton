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

uniform sampler2D samplerDepth;
uniform sampler2D samplerScene;

uniform float cocScale;
uniform float cocBias;

float calculate_coc(float depth) {
	return cocScale * depth + cocBias;
}

void main() {
	vec3 scene = texture(samplerScene, texCoord).xyz;
	
	vec4 depth = textureGather(samplerDepth, texCoord, 0);
	float coc = calculate_coc(min(max(depth.x, depth.y), max(depth.z, depth.w)));
	oColor = vec4(scene.xyz, coc);
}
#endif