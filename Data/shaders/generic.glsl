#version 150

#if VERTEX_SHADER

attrib(vec3, iPosition, Position)
attrib(vec3, iNormal, Normal)
attrib(vec3, iTangent, Tangent)
attrib(vec2, iTexCoord, TexCoord)

out vec3 normal;
out vec3 tangent;
out vec3 bitangent;
out vec2 texCoord;
out vec3 cameraDirection;

uniform(mat4x4, modelViewProjection, ModelViewProjection)
uniform(mat4x4, world, World)
uniform(vec3, cameraPosition, CameraPosition)

void main()
{
	texCoord = iTexCoord;
	
	normal = normalize((world * vec4(iNormal, 0)).xyz);
	tangent = normalize((world * vec4(iTangent, 0)).xyz);
	bitangent = cross(normal, tangent);
	
	vec3 worldPosition = (world * vec4(iPosition, 1)).xyz;
	
	cameraDirection = normalize(cameraPosition - worldPosition);
	
	gl_Position = modelViewProjection * vec4(iPosition, 1);
}

#else if FRAGMENT_SHADER

include(shaders/utility/utils)
include(shaders/lighting/cook_torrance)

in vec3 normal;
in vec3 tangent;
in vec3 bitangent;
in vec2 texCoord;
in vec3 cameraDirection;

out vec4 oColor;

uniform(vec3, lightDir)
uniform(vec3, lightColor)
uniform(vec3, ambientColor)

sampler(2D, samplerDiffuse, diffuse)
sampler(2D, samplerNormal, normal)
sampler(2D, samplerSpecular, specular)

void main()
{
	mat3x3 rot = mat3x3(tangent, bitangent, normal);

	vec3 N = (texture2D(samplerNormal, texCoord).xyz - 0.5f) * 2.0f;
	vec3 N2 = normalize(rot * N);
	
	vec3 reflectionVector = reflect(normalize(-cameraDirection), N2);
	vec4 diffuse = texture2D(samplerDiffuse, texCoord);

	float specularLevel = texture2D(samplerSpecular, texCoord).x;
	
	vec3 ambient = mix(ambientColor * 0.5f, ambientColor, saturate(N2.z * 0.5f + 0.5f));

	vec2 ls = cook_torrance(N2, normalize(cameraDirection), normalize(-lightDir), 0.6f, 0.6f);
	
	vec3 c = (ambientColor + ls.x) * (lightColor * ls.y + diffuse.xyz);
	
	oColor = vec4(c, 1.0f);
}
#endif