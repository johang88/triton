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

uniform sampler2D samplerScene;
uniform sampler2D samplerBokeh;
uniform sampler2D samplerColorCoc;

void main() {
	vec4 farPlaneColor = texture(samplerBokeh, texCoord * vec2(0.5, 1.0) + vec2(0.5, 0.0));
	vec4 nearPlaneColor = texture(samplerBokeh, texCoord * vec2(0.5, 1.0));
	
	vec4 origColor = texture(samplerScene, texCoord);
	vec4 downSampledColor = texture(samplerColorCoc, texCoord);
	
	float coc = downSampledColor.w;
	
	vec3 farColor = farPlaneColor.xyz / max(farPlaneColor.www, 0.0001);
	vec3 nearColor = nearPlaneColor.xyz / max(nearPlaneColor.www, 0.0001);
	
	vec3 blendedFarFocus = mix(downSampledColor.xyz, farColor, saturate(coc - 2.0));
	blendedFarFocus = mix(origColor.xyz, blendedFarFocus, saturate(0.5 * coc - 1.0));
	
	vec3 finalColor = mix(blendedFarFocus, nearColor, saturate(saturate(-coc - 1.0) + nearPlaneColor.w * 8.0));
	// finalColor = blendedFarFocus;
	
	oColor = vec4(finalColor, 1);
}
#endif