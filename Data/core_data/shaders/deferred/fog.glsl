import(/shaders/core);
#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_TEXCOORD_0) in vec2 iTexCoord;

out vec2 texCoord;

uniform mat4x4 modelViewProjection;

void main()
{
	texCoord = iTexCoord;
	gl_Position = vec4(iPosition, 1);
}

#else

in vec2 texCoord;

layout(location = 0) out vec4 oColor;

uniform sampler2D samplerScene;
uniform sampler2D samplerGBuffer2;

uniform vec2 screenSize;
uniform float fogStart;
uniform float fogEnd;
uniform vec3 fogColor;

void main()
{
	vec2 project = gl_FragCoord.xy / screenSize.xy;
	
	vec4 scene = texture2D(samplerScene, project);
	vec4 gbuffer2 = texture2D(samplerGBuffer2, project);
	
	vec3 position = gbuffer2.xyz;

	float depth = saturate((-position.z - fogStart) / (fogEnd - fogStart));
	
	oColor.xyz = mix(scene.xyz, fogColor, depth);;
	oColor.w = 1.0;
}
#endif