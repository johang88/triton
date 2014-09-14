// Attribs are handled using the C preprocessor
#define ATTRIB_POSITION 0
#define ATTRIB_NORMAL 1
#define ATTRIB_TANGENT 2
#define ATTRIB_TEXCOORD_0 3
#define ATTRIB_TEXCOORD_1 4
#define ATTRIB_COLOR 5
#define ATTRIB_BONE_INDEX 6
#define ATTRIB_BONE_WEIGHT 7
// NOTE: Is a 4x4 matrix so it takes up 4 slots
#define ATTRIB_INSTANCE_TRANSFORM 8

#define PI 3.14159265358979323846264
#define PI_OVER_FOUR PI/4
#define PI_OVER_TWO PI/2

float saturate(float value) {
	return clamp(value, 0.0, 1.0);
}

float square(float value) {
	return value * value;
}

vec3 encodeNormals(vec3 normals) {
	return normals * 0.5 + 0.5;
}

vec3 decodeNormals(vec3 normals) {
	return normals * 2.0 - 1.0;
}

vec3 encodeDiffuse(vec3 diffuse) {
	return sqrt(diffuse);
}

vec3 decodeDiffuse(vec3 diffuse) {
	return diffuse * diffuse;
}

uniform mat4x4 invViewProjection;
vec3 decodeWorldPosition(vec2 coord, float depth) {
	depth = depth * 2.0 - 1.0;
	
	vec3 clipSpacePosition = vec3(coord * 2.0 - 1.0, depth);
	vec4 worldPosition = invViewProjection * vec4(clipSpacePosition, 1);
	
	return worldPosition.xyz / worldPosition.w;
}