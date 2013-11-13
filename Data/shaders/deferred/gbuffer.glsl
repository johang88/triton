#ifdef VERTEX_SHADER

attrib(vec3, iPosition, Position);
attrib(vec2, iTexCoord, TexCoord);
attrib(vec3, iNormal, Normal);
attrib(vec3, iTangent, Tangent);

out vec3 normal;
out vec3 tangent;
out vec3 bitangent;
out vec2 texCoord;
out vec4 position;

uniform(mat4x4, world, World);
uniform(mat4x4, worldView, WorldView);
uniform(mat4x4, modelViewProjection, ModelViewProjection);

void main()
{
	texCoord = iTexCoord;
	
	normal = normalize(iNormal);
	tangent = normalize(iTangent);
	bitangent = normalize(cross(normal, tangent));
	
	position = worldView * vec4(iPosition, 1);
	
	gl_Position = modelViewProjection * vec4(iPosition, 1);
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
out(vec4, oSpecular, 3);

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