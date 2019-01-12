#include "/shaders/core"
#include "/shaders/brdf"

#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_TEXCOORD_0) in vec2 iTexCoord;

out vec2 texCoord;
out vec3 position;

void main() {
	texCoord = iTexCoord;
	gl_Position = vec4(iPosition, 1);
}

#else
in vec2 texCoord;
in vec3 position;

layout(location = 0) out vec4 oColor;

uniform sampler2D samplerDepth;
uniform sampler2D samplerScene;
uniform sampler2D samplerGBuffer1;

uniform vec3 cameraPosition;
uniform vec3 sunDirection = vec3(0.0249974, 0.8549658, -0.5180817);

const float earthRadius = 6360000;
const float atmosphereRadius = 6400000;
const float Hr = 7994;
const float Hm = 1200;

const vec3 betaR = vec3(0.0000038, 0.0000135, 0.0000331);
const vec3 betaM = vec3(0.000021, 0.000021, 0.000021);

bool solveQuadratic(float a, float b, float c, out float x1, out float x2) {
	if (b == 0) {
		if (a == 0) return false;
		x1 = 0; x2 = sqrt(-c / a);
		return true;
	}

	float discr = b * b - 4 * a * c;

	if (discr < 0) return false;

	float q = (b < 0.0) ? -0.5 * (b - sqrt(discr)) : -0.5 * (b + sqrt(discr));
	x1 = q / a;
	x2 = c / q;

	return true; 
}

bool raySphereIntersect(in vec3 orig, in vec3 dir, float radius, out float t0, out float t1) {
	float A = dir.x * dir.x + dir.y * dir.y + dir.z * dir.z;
	float B = 2 * (dir.x * orig.x + dir.y * orig.y + dir.z * orig.z);

	float C = orig.x * orig.x + orig.y * orig.y + orig.z * orig.z - radius * radius;

	if (!solveQuadratic(A, B, C, t0, t1)) 
		return false;

	if (t0 > t1) {
		float tmp = t0;
		t0 = t1;
		t1 = tmp;
	}

	return true;
}

vec3 computeIncidentLight(vec3 orig, vec3 dir, float tmin, float tmax) {
	float t0, t1;
	if (!raySphereIntersect(orig, dir, atmosphereRadius, t0, t1) || t1 < 0)
		return vec3(0);

	tmin = max(t0, tmin);
	tmax = min(tmax, t1);

	const uint numSamples = 16;
	const uint numSamplesLight = 8;

	float segmentLength = (tmax - tmin) / numSamples; 
	float tCurrent = tmin; 

	vec3 sumR = vec3(0);
	vec3 sumM = vec3(0);

	float opticalDepthR = 0, opticalDepthM = 0; 
	float mu = dot(dir, sunDirection);

	float phaseR = 3.0 / (16.0 * PI) * (1 + mu * mu); 
	float g = 0.76;

	float phaseM = 3.0 / (8.0 * PI) * ((1.0 - g * g) * (1.0 + mu * mu)) / ((2.0 + g * g) * pow(1.0 + g * g - 2.0 * g * mu, 1.5));  
	for (uint i = 0; i < numSamples; ++i) { 
		vec3 samplePosition = orig + dir * (tCurrent + segmentLength * 0.5);
		float height = length(samplePosition) - earthRadius;

		float hr = exp(-height / Hr) * segmentLength;
		float hm = exp(-height / Hm) * segmentLength;

		opticalDepthR += hr; 
		opticalDepthM += hm; 

		float t0Light, t1Light; 
		raySphereIntersect(samplePosition, sunDirection, atmosphereRadius, t0Light, t1Light); 

		float segmentLengthLight = t1Light / numSamplesLight;
		float tCurrentLight = 0; 
		float opticalDepthLightR = 0, opticalDepthLightM = 0; 

		uint j; 
		for (j = 0; j < numSamplesLight; ++j) { 
			vec3 samplePositionLight = samplePosition + (tCurrentLight + segmentLengthLight * 0.5) * sunDirection;
			float heightLight = length(samplePositionLight) - earthRadius; 

			if (heightLight < 0) break; 

			opticalDepthLightR += exp(-heightLight / Hr) * segmentLengthLight; 
			opticalDepthLightM += exp(-heightLight / Hm) * segmentLengthLight; 

			tCurrentLight += segmentLengthLight; 
		}

		if (j == numSamplesLight) {  
			vec3 tau = betaR * (opticalDepthR + opticalDepthLightR) + betaM * 1.1 * (opticalDepthM + opticalDepthLightM); 
			vec3 attenuation = vec3(exp(-tau.x), exp(-tau.y), exp(-tau.z)); 
			sumR += attenuation * hr; 
			sumM += attenuation * hm; 
		}

		tCurrent += segmentLength; 
	}

	return (sumR * betaR * phaseR + sumM * betaM * phaseM) * 20; 
}

void main() {
    vec4 gbuffer1 = texture2D(samplerGBuffer1, texCoord);

	float depth = texture(samplerDepth, texCoord).x;
	vec3 position = decodeWorldPosition(texCoord, depth);
	
    vec3 V = normalize(position - cameraPosition);

    vec3 P = cameraPosition + vec3(0, earthRadius, 0);

	if (gbuffer1.w == 0) {
        oColor.xyz = computeIncidentLight(P, V, 0, 9999999999.0);
	} else {
        float tmax = length(position);
        
        vec3 color = texture(samplerScene, texCoord).xyz;
        color += computeIncidentLight(P, V, 0, tmax);
		
        oColor.xyz = color;
	}

    oColor.w = 1.0;
}
#endif