#include "/shaders/core"

uniform float time;
uniform vec2 uvAnimation;

uniform mat4x4 itWorld;

#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_TEXCOORD_0) in vec2 iTexCoord;
#ifdef SPLAT
layout(location = ATTRIB_TEXCOORD_1) in vec2 iTexCoord2;
#endif
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
#ifdef SPLAT
out vec2 texCoordDetail;
#endif
out vec4 position;

uniform mat4x4 world;
uniform mat4x4 worldView;
uniform mat4x4 modelViewProjection;

#ifdef SKINNED
uniform mat4x4[96] bones;
#endif

void main()
{
	texCoord = iTexCoord;
#ifdef SPLAT
	texCoordDetail = iTexCoord2;
#endif
	
#ifdef ANIM_UV
	texCoord += uvAnimation * time;
#endif
	
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
	
	blendPos = vec4(blendPos.xyz, 1);
	
	normal = normalize(blendNormal);
	tangent = normalize(iTangent);
	bitangent = cross(normal, tangent);
	
	normal = mat3(itWorld) * normal;
	tangent = mat3(itWorld) * tangent;
	bitangent = mat3(itWorld) * bitangent;
	
	position = world * blendPos;

	gl_Position = modelViewProjection * blendPos;
#else
	normal = mat3(itWorld) * normalize(iNormal);
	tangent = mat3(itWorld) * normalize(iTangent);
	bitangent = normalize(cross(normal, tangent));
	
	position = world * vec4(iPosition, 1);

	gl_Position = modelViewProjection * vec4(iPosition, 1);
#endif
}

#else

#include "/shaders/brdf"

in vec3 normal;
in vec3 tangent;
in vec3 bitangent;
in vec2 texCoord;
#ifdef SPLAT
in vec2 texCoordDetail;
#endif
in vec4 position;

layout(location = 0) out vec4 oColor;
layout(location = 1) out vec4 oNormal;
layout(location = 2) out vec4 oSpecular;

uniform vec3 cameraPosition;

uniform sampler2D samplerDiffuseMap;
uniform sampler2D samplerDiffuseMapAlpha;
uniform sampler2D samplerNormalMap;
uniform sampler2D samplerNormalMapBC5;
uniform sampler2D samplerRoughnessMetalMap;
uniform sampler2D samplerOcclusionRoughnessMetalness;

uniform vec4 uDiffuseColor;
uniform float uRoughness;
uniform float uMetalness;

void get_material(out vec3 diffuse, out vec3 normals, out float metallic, out float specular, out float roughness, out float occlusion) {
#ifdef HAS_SAMPLER_DIFFUSEMAP
	diffuse = pow(texture(samplerDiffuseMap, texCoord).xyz, vec3(2.2));
#endif
#ifdef HAS_SAMPLER_DIFFUSEMAPALPHA
	vec4 D = texture(samplerDiffuseMap, texCoord);
	diffuse = pow(D.xyz, vec3(2.2));

	if (D.a < 0.5) discard;
#endif
#ifdef HAS_DIFFUSECOLOR
	diffuse = pow(uDiffuseColor.xyz, vec3(2.2));
#endif

	occlusion = 1.0;
	
#if defined(HAS_SAMPLER_NORMALMAP) || defined(HAS_SAMPLER_NORMALMAPBC5)
	#ifdef HAS_SAMPLER_NORMALMAP
	vec4 NR = texture(samplerNormalMap, texCoord);
	NR.xyz = normalize(NR.xyz * 2.0 - 1.0);
	#else
	vec4 NR = texture(samplerNormalMapBC5, texCoord);
	NR.z = sqrt(1 - NR.x * NR.x - NR.y * NR.y);
	NR.xyz = normalize(NR.xyz * 2.0 - 1.0);
	#endif

	mat3x3 TBN = mat3x3(normalize(tangent), normalize(bitangent), normalize(normal));
	normals = normalize(TBN * NR.xyz);

	#if !defined(HAS_SAMPLER_ROUGHNESSMETALMAP) && !defined(HAS_SAMPLER_OCCLUSIONROUGHNESSMETALNESS)
	roughness = NR.w;
	metallic = 0.0;
	#endif
#else
	normals = normalize(normal);
#endif
	
#ifdef HAS_SAMPLER_ROUGHNESSMETALMAP
	vec4 materialParameters = texture(samplerRoughnessMetalMap, texCoord);
	
	roughness = materialParameters.x;
	metallic = materialParameters.y;
#endif
#ifdef HAS_SAMPLER_OCCLUSIONROUGHNESSMETALNESS
	vec4 materialParameters = texture(samplerOcclusionRoughnessMetalness, texCoord);
	
	roughness = materialParameters.y;
	metallic = materialParameters.z;
	//occlusion = materialParameters.x;
#endif
#ifdef HAS_ROUGHNESS
	roughness = uRoughness;
#endif
#ifdef HAS_METALNESS
	metallic = uMetalness;
#endif

	specular = 0.5;
}

void main() {
	vec3 diffuse;
	vec3 normals;
	float metallic, specular, roughness, occlusion;

	get_material(diffuse, normals, metallic, specular, roughness, occlusion);
	roughness = max(0.01, roughness);

	oColor = vec4(encodeDiffuse(diffuse), 0);
	oNormal = vec4(encodeNormals(normals), 1);
	oSpecular = vec4(metallic, roughness, specular, occlusion);
	
#ifdef UNLIT
	oNormal.w = 0;
#endif
}
#endif