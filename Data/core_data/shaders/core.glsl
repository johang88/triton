// Attribs are handled using the C preprocessor
#define ATTRIB_POSITION 0
#define ATTRIB_NORMAL 1
#define ATTRIB_TANGENT 2
#define ATTRIB_TEXCOORD_0 3
#define ATTRIB_TEXCOORD_1 4
#define ATTRIB_COLOR 5
#define ATTRIB_BONE_INDEX 6
#define ATTRIB_BONE_WEIGHT 7

#define PI 3.14159265358979323846264
#define PI_OVER_FOUR PI/4
#define PI_OVER_TWO PI/2

float saturate(float value) {
	return clamp(value, 0.0, 1.0);
}

float square(float value) {
	return value * value;
}