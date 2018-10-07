#include "/shaders/core"
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

#include "/shaders/deferred/shadows"

in vec2 texCoord;

layout(location = 0) out vec4 oColor;

uniform sampler2D samplerDepth;
uniform vec2 screenSize;
uniform sampler2D[5] samplerShadowCsm;
uniform mat4x4[5] shadowViewProjCsm;
uniform float[6] shadowClipDistances;
uniform float texelSize;
uniform vec2 cameraClipPlane;

void main()
{
	vec2 project = gl_FragCoord.xy / screenSize.xy;
	
	float depth = texture(samplerDepth, project).x;
	if (depth == 1.0) 
		discard;
	
	vec3 position = decodeWorldPosition(project, depth);

	vec3 cascadeColor = vec3(1);
	float shadow = 1;
	
	float n = cameraClipPlane.x;
	float f = cameraClipPlane.y;
	
	float linearDepth = (2 * n) / (f + n - depth * (f - n));
	
	vec3 colors[5] = { 
		vec3(1, 0, 0), 
		vec3(0, 1, 0),
		vec3(0, 0, 1),
		vec3(1, 1, 0),
		vec3(1, 0, 1)
	};
	
	// Find the correct cascade
	for (int i = 0; i < 5; i++) {
		if (linearDepth > shadowClipDistances[i] && linearDepth < shadowClipDistances[i + 1]) {
			shadow = get_shadows(samplerShadowCsm[i], position, shadowViewProjCsm[i], texelSize);
			cascadeColor = colors[i];
			break;
		}
	}
	
	oColor.xyz = shadow.xxx;
	oColor.w = 1.0;
}
#endif