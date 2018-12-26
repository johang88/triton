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
uniform float tanHalfFov;
uniform vec3 cameraPosition;

float width = viewportResolution.x;
float height = viewportResolution.y;

const int samples = 32;
const float radius = 0.05;
const float aoclamp = 0.5; 

const float diffarea = 0.45;
const float gdisplace = 0.45;

vec2 rand(vec2 coord) {
	vec2 noiseScale = viewportResolution / vec2(4.0, 4.0);
	vec3 sampleNoise = texture(samplerNoise, texCoord * noiseScale).xyz;

	return sampleNoise.xy;
}

float readDepth(in vec2 coord)  {
	float n = cameraClipPlanes.x;
	float f = cameraClipPlanes.y;

	if (coord.x < 0.0|| coord.y < 0.0) return 1.0;
	
	return (2.0 * n) / (f + n - texture(samplerDepth, coord).x * (f - n));
}

float compareDepths(in float depth1, in float depth2,inout int far) {  
	float garea = 2.0; //gauss bell width    
	float diff = (depth1 - depth2)*100.0; //depth difference (0-100)

	//reduce left bell width to avoid self-shadowing 
	if (diff < gdisplace) {
		garea = diffarea;
	} else {
		far = 1;
	}

	float gauss = pow(2.7182,-2.0*(diff-gdisplace)*(diff-gdisplace)/(garea*garea));
	return gauss;
} 

float calAO(float depth,float dw, float dh)
{   
	float dd = (1.0-depth)*radius;

	float temp = 0.0;
	float temp2 = 0.0;
	float coordw = texCoord.x + dw*dd;
	float coordh = texCoord.y + dh*dd;
	float coordw2 = texCoord.x - dw*dd;
	float coordh2 = texCoord.y - dh*dd;

	vec2 coord = vec2(coordw , coordh);
	vec2 coord2 = vec2(coordw2, coordh2);

	int far = 0;
	temp = compareDepths(depth, readDepth(coord),far);
	//DEPTH EXTRAPOLATION:
	if (far > 0) {
		temp2 = compareDepths(readDepth(coord2),depth,far);
		temp += (1.0-temp)*temp2;
	}

	return temp;
} 

void main() {
	vec2 noise = rand(texCoord);
	float depth = readDepth(texCoord);

	float w = (1.0 / width) / clamp(depth, aoclamp, 1.0) + (noise.x* ( 1.0 - noise.x));
	float h = (1.0 / height) / clamp(depth, aoclamp, 1.0) + (noise.y * (1.0 - noise.y));

	float pw;
	float ph;

	float ao;

	float dl = PI * (3.0 - sqrt(5.0));
	float dz = 1.0 / float(samples);
	float l = 0.0;
	float z = 1.0 - dz / 2.0;

	for (int i = 0; i <= samples; i++) {
		float r = sqrt(1.0 - z);

		pw = cos(l) * r;
		ph = sin(l) * r;
		ao += calAO(depth, pw * w, ph * h);        
		z = z - dz;
		l = l + dl;
	}

	ao /= float(samples);
	ao = 1.0 - ao;

	oColor = vec4(clamp(pow(ao, 2.0), 0.0, 1.0).xxx, 1.0);
}

/*#define NUM_SAMPLES 32
#define NUM_SPIRAL_TURNS 3

const float radiusWS = 0.8;
const float bias = 0.1;
const float intensity = 10.0;

float linearDepth(float z) {
	float n = cameraClipPlanes.x;
	float f = cameraClipPlanes.y;
	
	return (2 * n) / (f + n - z * (f - n));
}

vec3 getPositionVS(vec2 uv) { 
	float depth = texture(samplerDepth, texCoord).x;
	vec3 positionWS = decodeWorldPosition(uv, depth);
	vec3 positionVS = (view * (vec4(positionWS, 1.0))).xyz;

	return positionVS;
}

vec3 reconstructNormalVS(vec3 positionVS) {
	return normalize(cross(dFdx(positionVS), dFdy(positionVS)));
}

vec2 tapLocation(int sampleNumber, float spinAngle, out float radiusSS) {
	float alpha = (float(sampleNumber) + 0.5) * (1.0 / float(NUM_SAMPLES));
	float angle = alpha * (float(NUM_SPIRAL_TURNS) * 6.28) + spinAngle;

	radiusSS = alpha;
	return vec2(cos(angle), sin(angle));
}

vec3 getOffsetPositionVS(vec2 uv, vec2 unitOffset, float radiusSS) {
	uv = uv + radiusSS * unitOffset * (1.0 / viewportResolution);
	return getPositionVS(uv);
}

float sampleAO(vec2 uv, vec3 positionVS, vec3 normalVS, float sampleRadiusSS, int tapIndex, float rotationAngle) {
	const float epsilon = 0.01;
	float radius2 = radiusWS * radiusWS;

	float radiusSS;
	vec2 unitOffset = tapLocation(tapIndex, rotationAngle, radiusSS);
	radiusSS *= sampleRadiusSS;

	vec3 Q = getOffsetPositionVS(uv, unitOffset, radiusSS);
	vec3 v = Q - positionVS;

	float vv = dot(v, v);
	float vn = dot(v, normalVS) - bias;

	float f = max(radius2 - vv, 0.0) / radius2;
	return f * f * f * max(vn / (epsilon + vv), 0.0);
}

void main() {
	vec3 originVS = getPositionVS(texCoord);
	//vec3 normalVS = reconstructNormalVS(originVS);
	vec3 normalWS = decodeNormals(texture(samplerGBuffer1, texCoord).xyz);
	vec3 normalVS = mat3(itView) * normalWS;

	vec2 noiseScale = viewportResolution / vec2(4.0, 4.0);
	vec3 sampleNoise = texture(samplerNoise, texCoord * noiseScale).xyz;

	float randomPatternRotationAngle = 2.0 * PI * sampleNoise.x;

	float projScale = 800;
	float radiusSS = projScale * radiusWS / originVS.y;

	float occlusion = 0.0;
	for (int i = 0; i < NUM_SAMPLES; ++i) {
		occlusion += sampleAO(texCoord, originVS, normalVS, radiusSS, i, randomPatternRotationAngle);
	}

	occlusion = 1.0 - occlusion / (4.0 * float(NUM_SAMPLES));
	occlusion = clamp(pow(occlusion, 1.0 + intensity), 0.0, 1.0);

	oColor.xyz = occlusion.xxx	;
	oColor.w = 1.0;
}*/
#endif