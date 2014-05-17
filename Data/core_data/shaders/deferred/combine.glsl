import(shaders/core);
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

import(shaders/utility/utils);

in vec2 texCoord;

layout(location = 0) out vec4 oColor;

sampler(2D, samplerLight, LightTexture);
sampler(2D, samplerSSAO, SSAOTexture);

uniform(vec3, ambientColor, AmbientColor);
uniform(vec2, screenSize, ScreenSize);

void main()
{
	vec2 project = gl_FragCoord.xy / screenSize.xy;
	
	vec3 light = texture2D(samplerLight, texCoord).xyz;
	float ssao = texture2D(samplerSSAO, texCoord).z;
	
	oColor = vec4(light * ssao, 1.0f);
}
#endif