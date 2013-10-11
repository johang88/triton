#ifdef VERTEX_SHADER

attrib(vec3, iPosition, Position);
attrib(vec2, iTexCoord, TexCoord);
attrib(vec3, iNormal, Normal);
attrib(vec3, iTangent, Tangent);
attrib(vec4, iBoneIndex, BoneIndex);
attrib(vec4, iBoneWeight, BoneWeight);

out vec3 normal;
out vec3 tangent;
out vec3 bitangent;
out vec2 texCoord;
out vec4 position;

uniform(mat4x4, modelViewProjection, ModelViewProjection);
uniform(mat4x4, world, World);
uniform(mat4x4[64], bones, Bones);

void main()
{
	texCoord = iTexCoord;
	
	vec4 blendPos = vec4(0, 0, 0, 0);
	vec3 blendNormal = vec3(0, 0, 0);
	
	for (int bone = 0; bone < 4; bone++)
	{
		int index = int(iBoneIndex[bone]);
		float weight = iBoneWeight[bone];
		
		mat4 worldMatrix = bones[index];
		blendPos += (worldMatrix * vec4(iPosition, 1)) * weight;
		
		mat3 worldRot = mat3(worldMatrix[0].xyz, worldMatrix[1].xyz, worldMatrix[2].xyz);
		blendNormal+= (worldRot * iNormal) * weight;
	}
	
	blendNormal = normalize(blendNormal);
	
	blendPos = vec4(blendPos.xyz, 1);
	
	normal = normalize((vec4(blendNormal, 0) * world).xyz);
	tangent = normalize((vec4(iTangent, 0) * world).xyz);
	bitangent = cross(normal, tangent);
	
	position = world * blendPos;

	gl_Position = modelViewProjection * blendPos;
}

#else

import(shaders/utility/utils);

in vec3 normal;
in vec3 tangent;
in vec3 bitangent;
in vec2 texCoord;
in vec4 position;

out(vec4, oColor, 0);
out(vec4, oNormal, 1);
out(vec4, oPosition, 2);

sampler(2D, samplerDiffuse, DiffuseTexture);
sampler(2D, samplerNormal, NormalMap);

void main()
{
	mat3x3 rot = mat3x3(tangent, bitangent, normal);

	vec3 N = (texture2D(samplerNormal, texCoord).xyz - 0.5f) * 2.0f;
	vec3 N2 = normalize(rot * N);

	vec4 diffuse = texture2D(samplerDiffuse, texCoord);
	
	oColor = vec4(diffuse.xyz, 1.0f);
	oNormal = vec4(N2.xyz, 1.0f);
	oPosition = position;
}
#endif