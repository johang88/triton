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

uniform mat4x4 world;
uniform mat4x4 worldView;
uniform mat4x4 modelViewProjection;

#ifdef SKINNED
uniform mat4x4[64] bones;
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

in vec3 normal;
in vec3 tangent;
in vec3 bitangent;
in vec2 texCoord;
in vec4 position;

layout(location = 0) out vec4 oColor;
layout(location = 1) out vec4 oNormal;
layout(location = 2) out vec4 oPosition;
layout(location = 3) out vec4 oSpecular;

uniform sampler2D samplerDiffuse;
uniform sampler2D samplerNormal;

uniform mat4x4 itWorldView;

uniform vec3 materialDiffuseColor;
uniform float materialMetallicValue;
uniform float materialSpecularValue;
uniform float materialRoughnessValue;

vec3 get_normals() {
#ifdef NORMAL_MAP
	mat3x3 rot = mat3x3(normalize(tangent), normalize(bitangent), normalize(normal));

	vec3 N = normalize(texture2D(samplerNormal, texCoord).xyz * 2.0f - 1.0f);
	vec3 N2 = normalize(rot * N);
	
	return N2;
#else
	return normal;
#endif
}

vec3 get_diffuse() {
#ifdef DIFFUSE_MAP
	vec3 diffuse =  texture2D(samplerDiffuse, texCoord).xyz;
#elif defined(MATERIAL_DIFFUSE_COLOR)
	vec3 diffuse = materialDiffuseColor;
#else
	vec3 diffuse = vec3(0.8, 0.8, 0.8);
#endif
	return pow(diffuse, vec3(2.2));
}

float get_metallic() {
#ifdef MATERIAL_METALLIC_VALUE
	return materialMetallicValue;
#else
	return 0.1;
#endif
}

float get_specular() {
#ifdef MATERIAL_SPECULAR_VALUE
	return materialSpecularValue;
#else
	return 0;
#endif
}

float get_roughness() {
#ifdef MATERIAL_ROUGHNESS_VALUE
	return materialRoughnessValue;
#else
	return 0.6;
#endif
}

void main() {
	vec3 normals = get_normals();
	normals = normalize(mat3x3(itWorldView) * normals);
	
	vec3 diffuse = get_diffuse();
	
	float metallic = get_metallic();
	float specular = get_specular();

	float roughness = get_roughness();

	oColor = vec4(diffuse, 1.0f);
	oNormal = vec4(normals, 1.0f);
	oSpecular = vec4(metallic, roughness, specular, 1);
	oPosition = position;
}
#endif