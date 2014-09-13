import(/shaders/core);
#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_NORMAL) in vec3 iNormal;
#ifdef SKINNED
layout(location = ATTRIB_BONE_INDEX) in vec4 iBoneIndex;
layout(location = ATTRIB_BONE_WEIGHT) in vec4 iBoneWeight;
#endif

uniform mat4x4 model;
#ifdef SKINNED
uniform mat4x4[64] bones;
#endif

void main()
{
	vec3 offset = normalize(iNormal) * 0.05;
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
	
	gl_Position = model * blendPos;
#else
	gl_Position = model * vec4(iPosition - offset, 1);
#endif
}

#elif defined(GEOMETRY_SHADER)

layout(triangles) in;
layout(triangle_strip, max_vertices=18) out;

uniform mat4x4 viewProjectionMatrices[6];

out vec4 position;

void main() {
	for (int layer = 0; layer < 6; ++layer) {
		gl_Layer = layer;
		for (int i = 0; i < 3; ++i) {
			position = gl_in[i].gl_Position;
			gl_Position = viewProjectionMatrices[layer] * position;
			EmitVertex();
		}
		EndPrimitive();
	}
}

#else

in vec4 position;

layout(location = 0) out float fragmentdepth;
uniform vec2 clipPlane;
uniform vec3 lightPosition;

void main()
{
	float distance = distance(position.xyz, lightPosition);
	distance = distance / (clipPlane.y - clipPlane.x);
	
	fragmentdepth = distance;
	gl_FragDepth = distance;
}
#endif