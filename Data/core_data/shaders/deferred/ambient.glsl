import(/shaders/core);
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
in vec2 texCoord;

layout(location = 0) out vec4 oColor;

uniform sampler2D samplerGBuffer0;
uniform sampler2D samplerGBuffer1;
uniform vec3 ambientColor;

void main()
{
	vec4 gbuffer0 = texture2D(samplerGBuffer0, texCoord);
	vec4 gbuffer1 = texture2D(samplerGBuffer1, texCoord);
	
	vec3 ambient = ambientColor;
	
	if (gbuffer1.w == 0) {
		ambient = vec3(1, 1, 1);
	}
	
	vec3 diffuse = decodeDiffuse(gbuffer0.xyz);
	
	oColor = vec4(ambient * diffuse, 1.0);
}
#endif