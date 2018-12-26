#include "/shaders/core"
#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_NORMAL) in vec3 iNormal;
layout(location = ATTRIB_TEXCOORD_0) in vec2 iTexCoord;
layout(location = ATTRIB_BONE_INDEX) in vec4 iBoneIndex;
layout(location = ATTRIB_BONE_WEIGHT) in vec4 iBoneWeight;

out vec4 position;
out vec2 texCoord;

uniform mat4x4[96] bones;
uniform mat4x4 world;

layout(std140, binding = 0) uniform PerFrameData {
	vec4 lightDirectionAndBias;
	mat4x4 view;
	mat4x4 projection;
};

void main()
{
#ifdef SKINNED
	vec4 blendPos = vec4(0, 0, 0, 0);
	vec3 blendNormal = vec3(0, 0, 0);

	for (int bone = 0; bone < 4; bone++)
	{
		int index = int(iBoneIndex[bone]);
		float weight = iBoneWeight[bone];
		
		mat4 worldMatrix = bones[index];
		blendPos += (worldMatrix * vec4(iPosition, 1)) * weight;

		mat3 worldRot = mat3(worldMatrix[0].xyz, worldMatrix[1].xyz, worldMatrix[2].xyz);
		blendNormal += (worldRot * iNormal) * weight;
	}
	
	vec3 positionWS = (world * blendPos).xyz;
	vec3 normal = normalize(blendNormal);
#else
	vec3 positionWS = (world * vec4(iPosition, 1)).xyz;
	vec3 normal = normalize(iNormal);
#endif

	mat3x3 itWorld = inverse(transpose(mat3x3(world)));

	vec3 N = itWorld * normal;
#ifdef POINT
	vec3 L = normalize(lightDirectionAndBias.xyz - positionWS);
#else
	vec3 L = normalize(lightDirectionAndBias.xyz);
#endif

	float nDotL = dot(N, L);
	float cosTheta = clamp(1.0 - nDotL, 0.0, 1.0);
	float bias = lightDirectionAndBias.w * tan(acos(cosTheta));
	bias = clamp(bias, 0, lightDirectionAndBias.w * 2.0);

	vec4 position = (projection * view) * vec4(positionWS, 1);
	position.z += bias;

	gl_Position = position;
	texCoord = iTexCoord;
}

#else

in vec2 texCoord;

layout(location = 0) out float dummyoutput;

void main()
{
	dummyoutput = 0.0;
}
#endif