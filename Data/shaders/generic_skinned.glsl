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
out vec3 cameraDirection;

uniform(mat4x4, modelViewProjection, ModelViewProjection);
uniform(mat4x4, world, World);
uniform(vec3, cameraPosition, CameraPosition);
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
	
	normal = normalize((world * vec4(blendNormal, 0)).xyz);
	tangent = normalize((world * vec4(iTangent, 0)).xyz);
	bitangent = cross(normal, tangent);
	
	vec3 worldPosition = (world * blendPos).xyz;
	
	cameraDirection = normalize(cameraPosition - worldPosition);
	
	gl_Position = modelViewProjection * blendPos;
}

#else

import(shaders/utility/utils);
import(shaders/lighting/cook_torrance);

in vec3 normal;
in vec3 tangent;
in vec3 bitangent;
in vec2 texCoord;
in vec3 cameraDirection;

out(vec4, oColor, 0);

uniform(vec3, lightDir, LightDir);
uniform(vec3, lightColor, LightColor);
uniform(vec3, ambientColor, AmbientColor);

sampler(2D, samplerDiffuse, DiffuseTexture);
sampler(2D, samplerNormal, NormalMap);
sampler(2D, samplerSpecular, SpecularMap);

void main()
{
	mat3x3 rot = mat3x3(tangent, bitangent, normal);

	vec3 N = (texture2D(samplerNormal, texCoord).xyz - 0.5f) * 2.0f;
	vec3 N2 = normalize(rot * N);
	
	vec3 reflectionVector = reflect(normalize(-cameraDirection), N2);
	vec4 diffuse = texture2D(samplerDiffuse, texCoord);
	
	diffuse = pow(diffuse, (2.2f).xxxx);
	
	vec3 ambient = mix(ambientColor * 0.5f, ambientColor, saturate(N2.z * 0.5f + 0.5f));
	
	vec3 specular = texture2D(samplerSpecular, texCoord).xyz;

	vec2 ls = cook_torrance(N2, normalize(cameraDirection), normalize(-lightDir), specular.x, 0.23f);
	
	vec3 c = (ambientColor + ls.x) * (lightColor * ls.y * specular.xyz + diffuse.xyz);

	c = pow(c, (1.0f / 2.2f).xxx);
	oColor = vec4(c, 1.0f);
}
#endif