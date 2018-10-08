#include "/shaders/core"

uniform float time;
uniform vec2 uvAnimation;

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
uniform mat4x4[64] bones;
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
		blendNormal+= (worldRot * iNormal) * weight;
	}
	
	blendPos = vec4(blendPos.xyz, 1);
	
	normal = normalize(blendNormal);
	tangent = normalize(iTangent);
	bitangent = cross(normal, tangent);
	position = world * blendPos;

	gl_Position = modelViewProjection * blendPos;
#else
	normal = normalize(iNormal);
	tangent = normalize(iTangent);
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

uniform mat4x4 itWorld;

uniform vec3 cameraPosition;

uniform sampler2D sampler_2;
uniform sampler2D sampler_3;
vec4 get_diffuse() {
vec4 v_0 = texture(sampler_2, texCoord);
return pow(v_0, vec4(2.2));

}
vec3 get_normals() {
vec4 v_1 = texture(sampler_3, texCoord);
mat3x3 tbn_3 = mat3x3(normalize(tangent), normalize(bitangent), normalize(normal));
vec3 normals_2 = normalize(tbn_3 * normalize(v_1.xyz * 2.0 - 1.0));
return normals_2;

}
float get_metallic() {
float f_4 = 1;
return f_4;

}
float get_roughness() {
float f_5 = 0.4;
return f_5;

}
float get_specular() {
return 0.5;
}

/*vec3 get_normals() {
}*/

/*vec4 get_diffuse() {
}*/

/*float get_metallic() {
}*/

/*float get_specular() {
}*/

/*float get_roughness() {
}*/

void main() {
	vec3 normals = get_normals();
	
	normals = normalize(mat3x3(itWorld) * normals);
	
	vec3 diffuse = get_diffuse().xyz;
	
	float metallic = get_metallic();
	float specular = get_specular();

	float roughness = get_roughness();
	
	oColor = vec4(encodeDiffuse(diffuse), 0);
	oNormal = vec4(encodeNormals(normals), 1);
	oSpecular = vec4(metallic, roughness, specular, 0);
	
#ifdef UNLIT
	oNormal.w = 0;
#endif
}
#endif