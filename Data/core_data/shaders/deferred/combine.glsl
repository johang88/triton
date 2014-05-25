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

in vec2 texCoord;

layout(location = 0) out vec4 oColor;

uniform sampler2D samplerLight;
uniform vec3 ambientColor;
uniform vec2 screenSize;

void main()
{
	vec2 project = gl_FragCoord.xy / screenSize.xy;
	
	vec3 light = texture(samplerLight, texCoord).xyz;
	oColor = vec4(light, 1.0);
}
#endif