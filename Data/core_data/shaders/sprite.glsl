import(shaders/core);
#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_TEXCOORD_0) in vec2 iTexCoord;
layout(location = ATTRIB_COLOR) in vec4 iColor;

uniform mat4x4 modelViewProjection;

out vec2 texCoord;
out vec4 color;

void main()
{
	texCoord = iTexCoord;
	color = iColor;
	gl_Position = modelViewProjection * vec4(iPosition, 1);
}

#else

in vec2 texCoord;
in vec4 color;

layout(location = 0) out vec4 oColor;

uniform sampler2D samplerDiffuse;

void main()
{
	vec4 diffuse = texture2D(samplerDiffuse, texCoord) * color;
	
#ifdef SRGB
	oColor.xyz = pow(diffuse.xyz, vec3(1.0 / 2.2));
	oColor.a = diffuse.a;
#else
	oColor = diffuse;
#endif
}
#endif