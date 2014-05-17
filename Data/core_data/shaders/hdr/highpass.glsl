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

uniform sampler2D samplerScene;

uniform vec3 whitePoint;
uniform float exposure;

void main()
{
	vec3 scene = texture2D(samplerScene, texCoord).xyz * exposure;

	vec3 final = max(scene - whitePoint, vec3(0, 0, 0));
	
	oColor = vec4(final, 1.0f);
}
#endif