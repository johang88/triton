#include "/shaders/core"
#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_COLOR) in vec3 iColor;
layout(location = ATTRIB_NORMAL) in vec3 iNormal;

out vec3 color;

uniform mat4x4 modelViewProjection;

void main()
{
	vec3 L = vec3(0.1, 0.8, 0.3);
	float nDotL = saturate(dot(normalize(iNormal), normalize(L))) * 0.5 + 0.5;
	
	color = iColor * nDotL;
	
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