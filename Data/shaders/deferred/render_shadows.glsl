#ifdef VERTEX_SHADER

attrib(vec3, iPosition, Position);
#ifdef SKINNED
attrib(vec4, iBoneIndex, BoneIndex);
attrib(vec4, iBoneWeight, BoneWeight);
#endif

out vec4 position;

uniform(mat4x4, modelViewProjection, ModelViewProjection);
#ifdef SKINNED
uniform(mat4x4[64], bones, Bones);
#endif

void main()
{
#ifdef SKINNED
	vec4 blendPos = vec4(0, 0, 0, 0);

	for (int bone = 0; bone < 4; bone++)
	{
		int index = int(iBoneIndex[bone]);
		float weight = iBoneWeight[bone];
		
		mat4 worldMatrix = bones[index];
		blendPos += (worldMatrix * vec4(iPosition, 1)) * weight;
	}
	
	blendPos = vec4(blendPos.xyz, 1);
	
	position = modelViewProjection * blendPos;
	gl_Position = modelViewProjection * blendPos;
#else
	position = modelViewProjection * vec4(iPosition, 1);
	gl_Position = modelViewProjection * vec4(iPosition, 1);
#endif
}

#else

in vec4 position;

out(float, fragmentdepth, 0);
uniform(vec2, clipPlane, ClipPlane);

void main()
{
	float depth = position.z / (clipPlane.y - clipPlane.x);
	fragmentdepth = depth;
	gl_FragDepth = depth;
}
#endif