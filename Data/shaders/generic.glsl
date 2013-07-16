#ifdef VERTEX_SHADER

attrib(vec3, iPosition, Position);
attrib(vec2, iTexCoord, TexCoord);
attrib(vec3, iNormal, Normal);
attrib(vec3, iTangent, Tangent);

out vec3 normal;
out vec3 tangent;
out vec3 bitangent;
out vec2 texCoord;
out vec3 cameraDirection;
out vec3 lightDir;

uniform(mat4x4, modelViewProjection, ModelViewProjection);
uniform(mat4x4, world, World);
uniform(vec3, cameraPosition, CameraPosition);
uniform(vec3, lightPosition, LightDir);

void main()
{
	texCoord = iTexCoord;
	
	normal = normalize((world * vec4(iNormal, 0)).xyz);
	tangent = normalize((world * vec4(iTangent, 0)).xyz);
	bitangent = cross(normal, tangent);
	
	vec3 worldPosition = (world * vec4(iPosition, 1)).xyz;
	
	cameraDirection = normalize(cameraPosition - worldPosition);
	
	lightDir = lightPosition.xyz - worldPosition.xyz;
	
	gl_Position = modelViewProjection * vec4(iPosition, 1);
}

#else

import(shaders/utility/utils);
import(shaders/lighting/cook_torrance);

in vec3 normal;
in vec3 tangent;
in vec3 bitangent;
in vec2 texCoord;
in vec3 cameraDirection;
in vec3 lightDir;

out(vec4, oColor, 0);

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

	vec3 l = normalize(lightDir);
	vec2 ls = cook_torrance(N2, normalize(cameraDirection), l, specular.x, 0.23f);
	
	float attenuation = length(lightDir) / 5.0f;
	attenuation = saturate(1.0f - (attenuation * attenuation));
	ls = ls * attenuation.xx;
	
	vec3 c = (ambient + ls.x) * (lightColor * ls.y * specular.xyz + diffuse.xyz);

	c = pow(c, (1.0f / 2.2f).xxx);
	oColor = vec4(c, 1.0f);
}
#endif