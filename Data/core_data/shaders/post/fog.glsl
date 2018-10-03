#include "/shaders/core"
#include "/shaders/post/postcommon"
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

uniform sampler2D samplerDepth;
uniform sampler2D samplerScene;
uniform vec3 cameraPosition;
uniform vec3 sunDir;

void main() {
	vec3 sceneColor = texture(samplerScene, texCoord).xyz;
	float depth = texture(samplerDepth, texCoord).x;
	
	if (depth == 1.0) {
		oColor = vec4(sceneColor, 1.0);
	} else {
		vec3 position = decodeWorldPosition(texCoord, depth);

		float distance = distance(position, cameraPosition);
		vec3 eyeDir = normalize(cameraPosition - position);
		
		float b = 0.001;
		float fogAmount = 1.0 - exp(-distance * b);
		
		float sunAmount = max(dot(eyeDir, sunDir), 0.0);
		
		vec3 fogColor = mix(vec3(0.5,0.6,0.7) * 5, vec3(1.0,0.9,0.7) * 10, pow(sunAmount, 8.0));
		
		vec3 res = mix(sceneColor, fogColor, fogAmount);
		
		oColor = vec4(res, 1.0);
	}
}
#endif