#include "/shaders/core"
#include "/shaders/post/postcommon"

#ifdef VERTEX_SHADER
layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_TEXCOORD_0) in vec2 iTexCoord;

out vec2 texCoord;

void main() {
	texCoord = iTexCoord;
	gl_Position = vec4(iPosition, 1);
}
#else
in vec2 texCoord;

layout(location = 0) out vec4 oColor;

layout(binding = 0) uniform sampler2D samplerSSAO;

void main() {
	vec2 texelSize = vec2(1.0) / vec2(textureSize(samplerSSAO, 0));

    float result = 0.0;
    for (int x = -2; x < 2; ++x) {
        for (int y = -2; y < 2; ++y)  {
            vec2 offset = vec2(float(x), float(y)) * texelSize;
            result += texture(samplerSSAO, texCoord + offset).x;
        }
    }

    oColor.xyz = (result / (4.0 * 4.0)).xxx;
    oColor.w = 1.0;
}
#endif