import(/shaders/core);
#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_NORMAL) in vec3 iNormal;
#ifdef SKINNED
layout(location = ATTRIB_BONE_INDEX) in vec4 iBoneIndex;
layout(location = ATTRIB_BONE_WEIGHT) in vec4 iBoneWeight;
#endif

out vec4 position;

uniform mat4x4 modelViewProjection;
#ifdef SKINNED
uniform mat4x4[64] bones;
#endif

uniform float shadowBias = 0.05;

void main()
{
	vec3 offset = normalize(iNormal) * shadowBias;
	
#ifdef SKINNED
	vec4 blendPos = vec4(0, 0, 0, 0);

	for (int bone = 0; bone < 4; bone++)
	{
		int index = int(iBoneIndex[bone]);
		float weight = iBoneWeight[bone];
		
		mat4 worldMatrix = bones[index];
		blendPos += (worldMatrix * vec4(iPosition, 1)) * weight;
	}
	
	blendPos = vec4(blendPos.xyz - offset, 1);
	
	position = modelViewProjection * blendPos;
	gl_Position = modelViewProjection * blendPos;
#else
	position = modelViewProjection * vec4(iPosition - offset, 1);
	gl_Position = modelViewProjection * vec4(iPosition - offset, 1);
#endif
}

#else

in vec4 position;

layout(location = 0) out float fragmentdepth;
uniform vec2 clipPlane;

void main()
{
	float depth = position.z / position.w;
	fragmentdepth = depth;
	gl_FragDepth = depth;
}
#endif