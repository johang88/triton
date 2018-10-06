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

uniform sampler2D samplerScene;
uniform sampler2D samplerBloom;
uniform sampler2D samplerLuminance;

uniform bool enableBloom = true;
uniform float bloomStrength = 10.0;

void main() {
	float averageLuminance = get_average_luminance(samplerLuminance);
	vec3 color = texture(samplerScene, texCoord).xyz;

	color = tonemap(color, averageLuminance, 0);
	
	if (enableBloom) {
		vec3 bloom = texture(samplerBloom, texCoord).xyz;
		color += max(vec3(0), bloom) * bloomStrength;
	}

	oColor = vec4(color, 1);
}
#endif