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
#include "/shaders/brdf"

in vec2 texCoord;

layout(location = 0) out vec4 oColor;

uniform sampler2D samplerGBuffer0;
uniform sampler2D samplerGBuffer1;
uniform sampler2D samplerGBuffer2;
uniform sampler2D samplerGBuffer3;
uniform sampler2D samplerSSAO;
uniform sampler2D samplerSMAAEdge;
uniform sampler2D samplerSMAABlend;
uniform sampler2D samplerCSM;
uniform int visualizationMode;
uniform vec2 cameraClipPlanes;

float linearDepth(float zw) {
	float n = cameraClipPlanes.x;
	float f = cameraClipPlanes.y;

	return (2.0 * n) / (f + n - zw * (f - n));
}

void main() {
	vec4 gbuffer0 = texture(samplerGBuffer0, texCoord);
	vec4 gbuffer1 = texture(samplerGBuffer1, texCoord);
	vec4 gbuffer2 = texture(samplerGBuffer2, texCoord);
	vec4 gbuffer3 = texture(samplerGBuffer3, texCoord);
	vec4 ssao = texture(samplerSSAO, texCoord);
	vec4 smaaEdge = texture(samplerSMAAEdge, texCoord);
	vec4 smaaBlend = texture(samplerSMAABlend, texCoord);
	vec4 csm = texture(samplerCSM, texCoord);

	vec3 res = vec3(0);

	switch (visualizationMode) {
		case 1: // diffuse
			res = gbuffer0.xyz;
			break;
		case 2: // depth
			float depth = linearDepth(gbuffer3.x);
			res = depth.xxx;
			break;
		case 3: // normal
			res = decodeNormals(gbuffer1.xyz) * 0.5 + 0.5;
			break;
		case 4: // roughness
			res = gbuffer2.yyy;
			break;
		case 5: // metallic
			res = gbuffer2.xxx;
			break;
		case 6: // specular
			res = gbuffer2.zzz;
			break;
		case 7: // ssao
			res = ssao.xyz;
			break;
		case 8: // occlusion
			res = gbuffer2.www;
			break;
		case 9: // smaa edge
			res = smaaEdge.xyz;
			break;
		case 10: // smaa blend
			res = smaaBlend.xyz;
			break;
		case 11: // csm
			res = csm.xyz;
			break;
	}

	oColor = vec4(res, 1);
}
#endif