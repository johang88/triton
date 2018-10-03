import(/shaders/core);
#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec2 iPosition;
layout(location = ATTRIB_TEXCOORD_0) in vec2 iTexCoord;
layout(location = ATTRIB_COLOR) in vec4 iColor;

uniform mat4x4 modelViewProjection;

out vec2 texCoord;
out vec4 color;

void main()
{
	texCoord = iTexCoord;
	color = iColor;
	gl_Position = modelViewProjection * vec4(iPosition.xy, 0, 1);
}

#else

in vec2 texCoord;
in vec4 color;

layout(location = 0) out vec4 oColor;

uniform sampler2D samplerDiffuse;

void main()
{
	oColor = color * texture(samplerDiffuse, texCoord);
}
#endif