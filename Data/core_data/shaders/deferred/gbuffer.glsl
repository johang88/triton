import(shaders/core);
#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_TEXCOORD_0) in vec2 iTexCoord;
layout(location = ATTRIB_NORMAL) in vec3 iNormal;
layout(location = ATTRIB_TANGENT) in vec3 iTangent;

#ifdef SKINNED
layout(location = ATTRIB_BONE_INDEX) in vec4 iBoneIndex;
layout(location = ATTRIB_BONE_WEIGHT) in vec4 iBoneWeight;
#endif

out vec3 normal;
out vec3 tangent;
out vec3 bitangent;
out vec2 texCoord;
out vec4 position;

uniform(mat4x4, world, World);
uniform(mat4x4, worldView, WorldView);
uniform(mat4x4, modelViewProjection, ModelViewProjection);

#ifdef SKINNED
uniform(mat4x4[64], bones, Bones);
#endif

void main()
{
	texCoord = iTexCoord;
	
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
		blendNormal+= (worldRot * iNormal) * weight;
	}
	
	blendPos = vec4(blendPos.xyz, 1);
	
	normal = normalize(blendNormal);
	tangent = normalize(iTangent);
	bitangent = cross(normal, tangent);
	position = worldView * blendPos;

	gl_Position = modelViewProjection * blendPos;
#else
	normal = normalize(iNormal);
	tangent = normalize(iTangent);
	bitangent = normalize(cross(normal, tangent));
	
	position = worldView * vec4(iPosition, 1);
	
	gl_Position = modelViewProjection * vec4(iPosition, 1);
#endif
}

#else

import(shaders/utility/utils);

in vec3 normal;
in vec3 tangent;
in vec3 bitangent;
in vec2 texCoord;
in vec4 position;

layout(location = 0) out vec4 oColor;
layout(location = 1) out vec4 oNormal;
layout(location = 2) out vec4 oPosition;
layout(location = 3) out vec4 oSpecular;

sampler(2D, samplerDiffuse, DiffuseTexture);
sampler(2D, samplerNormal, NormalMap);
sampler(2D, samplerSpecular, SpecularMap);

uniform(mat4x4, itWorldView, ITWorldView);

void main()
{
	mat3x3 rot = mat3x3(normalize(tangent), normalize(bitangent), normalize(normal));

	vec3 N = normalize(texture2D(samplerNormal, texCoord).xyz * 2.0f - 1.0f);
	vec3 N2 = normalize(rot * N);
	N2 = normalize(mat3x3(itWorldView) * N2);

	vec4 diffuse = texture2D(samplerDiffuse, texCoord);
	
	vec4 specular = texture2D(samplerSpecular, texCoord);
	
	vec3 gamma = (2.2f).xxx;
	diffuse.xyz = pow(diffuse.xyz, gamma);
	specular.xyz = pow(specular.xyz, gamma);
	
	oColor = vec4(diffuse.xyz, 1.0f);
	oNormal = vec4(N2.xyz, 1.0f);
	oSpecular = specular;
	oPosition = position;
}
#endif