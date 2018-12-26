#include "/shaders/core"
#include "/shaders/post/postcommon"

#ifdef VERTEX_SHADER
layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_TEXCOORD_0) in vec2 iTexCoord;

out vec2 texCoord;

void main() {
	texCoord = iTexCoord;
	gl_Position = vec4(iPosition, 1);
}
#else
in vec2 texCoord;

layout(location = 0) out vec4 oColor;

uniform sampler2D samplerDepth;
uniform sampler2D samplerGBuffer1;
uniform sampler2D samplerNoise;

uniform mat4x4 view;
uniform mat4x4 proj;
uniform mat4x4 itView;
uniform vec2 viewportResolution;
uniform vec2 cameraClipPlanes;
uniform vec3[64] sampleKernel;

float width = viewportResolution.x;
float height = viewportResolution.y;

float linearDepth(float z) {
	float n = cameraClipPlanes.x;
	float f = cameraClipPlanes.y;
	
	return (2 * n) / (f + n - z * (f - n));
}

vec2 rand(vec2 coord) {
	float noiseX = ((fract(1.0-coord.s*(width/2.0))*0.25)+(fract(coord.t*(height/2.0))*0.75))*2.0-1.0;
	float noiseY = ((fract(1.0-coord.s*(width/2.0))*0.75)+(fract(coord.t*(height/2.0))*0.25))*2.0-1.0;

	noiseX = clamp(fract(sin(dot(coord ,vec2(12.9898,78.233))) * 43758.5453),0.0,1.0)*2.0-1.0;
	noiseY = clamp(fract(sin(dot(coord ,vec2(12.9898,78.233)*2.0)) * 43758.5453),0.0,1.0)*2.0-1.0;

	return vec2(noiseX,noiseY)*0.0002;
}

void main() {
	vec2 noiseScale = viewportResolution / vec2(4.0, 4.0);

	float depth = texture(samplerDepth, texCoord).x;
	vec3 fragPos = decodeViewPosition(texCoord, depth);
	//fragPos.z = linearDepth(depth);
	
	// float fragPos = linearDepth(texture(samplerDepth, texCoord).x);
	vec3 normal = normalize(mat3(itView) * decodeNormals(texture(samplerGBuffer1, texCoord).xyz));
	vec3 randomVec = texture(samplerNoise, texCoord * noiseScale).xyz;

	vec3 tangent = normalize(randomVec - normal * dot(randomVec, normal));
	vec3 bitangent = cross(normal, tangent);
	mat3 TBN = mat3(tangent, bitangent, normal);
	
	const float radius = 8.0;
	
	float occlusion = 0.0;
	for (int i = 0; i < 64; ++i) {
		vec3 ssample = TBN * sampleKernel[i];
		ssample = fragPos + ssample * radius;
		
		vec4 offset = vec4(ssample, 1.0);
		offset = proj * offset;
		offset.xyz /= offset.w;
		offset.xyz = offset.xyz * 0.5 + 0.5;
		
		float sampleDepth = texture(samplerDepth, offset.xy).x;
        vec3 sampleFragPos = decodeViewPosition(texCoord, depth);
		
		float rangeCheck = smoothstep(0.0, 1.0, radius / abs(fragPos.z - sampleFragPos.z));
		occlusion += (sampleFragPos.z >= ssample.z ? 1.0 : 0.0) * rangeCheck;
	}
	
	occlusion = 1.0 - (occlusion / 64.0);
	
	oColor.xyz = occlusion.xxx;
	oColor.a = 1;
}
#endif