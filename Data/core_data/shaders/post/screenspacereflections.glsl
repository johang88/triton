#include "/shaders/core"
#include "/shaders/post/postcommon"
#include "/shaders/brdf"
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

uniform float cameraNearClipDistance;
uniform mat4x4 itView;
uniform mat4x4 projectionMatrix;
// uniform vec3 cameraPosition;
uniform vec2 cameraClipPlane;
uniform sampler2D samplerScene;
uniform sampler2D samplerNormal;
uniform sampler2D samplerDepth;
uniform sampler2D samplerGBuffer2;

float getDepthAt(vec2 uv) {
	float depth = texture(samplerDepth, uv).x;
	
	// depth = depth * 2.0 - 1.0;
	
	float n = cameraClipPlane.x;
	float f = cameraClipPlane.y;
	
	return (2 * n) / (f + n - depth * (f - n));
	// return depth;
}

vec3 raytrace(in vec3 reflectionVector, in float startDepth) {
	vec3 color = vec3(0, 0, 0);
	
	float stepSize = 0.01; // Uniform this
	
	float size = length(reflectionVector);
	reflectionVector = normalize(reflectionVector / size);
	reflectionVector *= stepSize;
	
	vec2 uv = texCoord;
	float currentDepth = startDepth;
	
	int samples = 0;
	while (uv.x >= 0.0 && uv.x <= 1.0 && uv.y >= 0.0 && uv.y <= 1.0) {
		samples++;
		uv += reflectionVector.xy;
		
		currentDepth += reflectionVector.z * startDepth * 0.1;
		float sampledDepth = getDepthAt(uv);
		
		if (currentDepth > sampledDepth) {
			float delta = currentDepth - sampledDepth;
			if (delta < 0.001) {
				color = texture(samplerScene, uv).xyz;
				return color;
			}
		}
		
		if (samples >= 64)
			break;
	}
	
	return color;
}

void main() {
	vec4 gbuffer2 = texture(samplerGBuffer2, texCoord); // specular stuff
	vec3 color = texture(samplerScene, texCoord).xyz;
	vec3 normal = (itView * vec4(decodeNormals(texture(samplerNormal, texCoord).xyz), 0)).xyz;
	float roughness = 1.0 - gbuffer2.y;
	
	float depth = getDepthAt(texCoord);
	
	vec3 cameraPosition = normalize(vec3(0, 0, cameraClipPlane.x));
	vec3 reflectionVector = (projectionMatrix * vec4(reflect(-cameraPosition, normal), 0)).xyz;
	
	vec3 reflectedColor = raytrace(reflectionVector, depth);
	
	float F = saturate(roughness * 1.0 - abs(dot(cameraPosition, normal)));

	oColor.xyz = color + reflectedColor * F;
	oColor.w = 1;
}
#endif