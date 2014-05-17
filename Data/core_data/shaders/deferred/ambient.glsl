import(shaders/core);
#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_TEXCOORD_0) in vec2 iTexCoord;

out vec2 texCoord;

uniform mat4x4 modelViewProjection;

void main()
{
	texCoord = iTexCoord;
	
	gl_Position = modelViewProjection * vec4(iPosition, 1);
}

#else

import(shaders/utility/utils);

in vec2 texCoord;

layout(location = 0) out vec4 oColor;

uniform sampler2D samplerDiffuse;
uniform vec3 ambientColor;

void main()
{
	vec3 diffuse = texture2D(samplerDiffuse, texCoord).xyz;

	oColor = vec4(diffuse * ambientColor, 1.0f);
}
#endif