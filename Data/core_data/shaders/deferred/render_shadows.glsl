#include "/shaders/core"
#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_NORMAL) in vec3 iNormal;
layout(location = ATTRIB_TEXCOORD_0) in vec2 iTexCoord;
layout(location = ATTRIB_BONE_INDEX) in vec4 iBoneIndex;
layout(location = ATTRIB_BONE_WEIGHT) in vec4 iBoneWeight;
layout(location = ATTRIB_INSTANCE_TRANSFORM) in mat4x4 iInstanceTransform;

out vec4 position;
out vec2 texCoord;
out float depth;

layout(location = 0) uniform vec4 lightDirectionAndBias;
uniform mat4x4[96] bones;

void main()
{
	vec3 N = normalize(iNormal);
	vec3 L = -normalize(lightDirectionAndBias.xyz);

	float nDotL = dot(N, L);
	float cosTheta = clamp(nDotL, 0.0, 1.0);
	float bias = lightDirectionAndBias.w * tan(acos(cosTheta));
	bias = clamp(bias, lightDirectionAndBias.w * 0.1, lightDirectionAndBias.w * 2.0);

#ifdef SKINNED
	vec4 blendPos = vec4(0, 0, 0, 0);

	for (int bone = 0; bone < 4; bone++)
	{
		int index = int(iBoneIndex[bone]);
		float weight = iBoneWeight[bone];
		
		mat4 worldMatrix = bones[index];
		blendPos += (worldMatrix * vec4(iPosition, 1)) * weight;
	}
	
	position = iInstanceTransform * blendPos;
#else
	position = iInstanceTransform * vec4(iPosition, 1);
#endif

	depth = position.z + bias;

	gl_Position = position;
	texCoord = iTexCoord;
}

#else

in vec2 texCoord;
in float depth;

layout(location = 0) out vec3 oColor;

void main()
{
	oColor = depth.xxx;
}
#endif