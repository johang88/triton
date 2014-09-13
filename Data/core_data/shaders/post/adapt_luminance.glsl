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

uniform sampler2D samplerLastLuminacne;
uniform sampler2D samplerCurrentLuminance;

uniform float tau;
uniform float timeDelta;

void main()
{
	float lastLuminance = texelFetch(samplerLastLuminacne, ivec2(0, 0), 0).x;
	float currentLuminance = exp(textureLod(samplerCurrentLuminance, vec2(0.5, 0.5), 10).x);
	
	float adaptedLuminance = lastLuminance + (currentLuminance - lastLuminance) * (1 - exp(-timeDelta * tau));
	
	oColor = vec4(adaptedLuminance, 1, 1, 1);
}
#endif