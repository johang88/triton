import(/shaders/core);
#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_COLOR) in vec3 iColor;

out vec3 color;

uniform mat4x4 modelViewProjection;

void main()
{
	color = iColor;
	gl_Position = modelViewProjection * vec4(iPosition, 1);
}

#else

in vec3 color;

layout(location = 0) out vec4 oColor;

void main()
{
	oColor = vec4(color, 1.0);
}
#endif