#include "/shaders/core"

#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_TEXCOORD_0) in vec2 iTexCoord;
layout(location = ATTRIB_NORMAL) in vec3 iNormal;
layout(location = ATTRIB_TANGENT) in vec3 iTangent;

out vec3 normal;
out vec3 tangent;
out vec3 bitangent;
out vec2 texCoord;
out vec2 texCoordDetail;
out vec4 position;

uniform mat4x4, world;
uniform mat4x4, worldView;
uniform mat4x4, modelViewProjection;

void main()
{
	texCoord = iTexCoord;
	texCoordDetail = iTexCoord * 32;
	
	normal = normalize(iNormal);
	tangent = normalize(iTangent);
	bitangent = normalize(cross(normal, tangent));
	
	position = worldView * vec4(iPosition, 1);
	
	gl_Position = modelViewProjection * vec4(iPosition, 1);
}

#else

in vec3 normal;
in vec3 tangent;
in vec3 bitangent;
in vec2 texCoord;
in vec2 texCoordDetail;
in vec4 position;

layout(location = 0) out vec4 oColor;
layout(location = 1) out vec4 oNormal;
layout(location = 2) out vec4 oPosition;
layout(location = 3) out vec4 oSpecular;
layout(location = 4) out vec4 oEmissive;

uniform sampler2D samplerSplat;
uniform sampler2D samplerNormal1;
uniform sampler2D samplerNormal2;
uniform sampler2D samplerNormal3;
uniform sampler2D samplerNormal4;
uniform sampler2D samplerDiffuse1;
uniform sampler2D samplerDiffuse2;
uniform sampler2D samplerDiffuse3;
uniform sampler2D samplerDiffuse4;

uniform mat4x4 itWorldView;

void main()
{
	vec4 splat = texture2D(samplerSplat, texCoord);
	
	vec4 n1 = texture2D(samplerNormal1, texCoordDetail);
	vec4 n2 = texture2D(samplerNormal2, texCoordDetail);
	vec4 n3 = texture2D(samplerNormal3, texCoordDetail);
	vec4 n4 = texture2D(samplerNormal4, texCoordDetail);
	
	vec4 normalSpecular = n1 * splat.x + n2 * splat.y + n3 * splat.z + n4 * splat.w;
	vec3 N = normalize(normalSpecular.xyz * 2.0 - 1.0);

	mat3x3 rot = mat3x3(normalize(tangent), normalize(bitangent), normalize(normal));

	vec3 N2 = normalize(rot * N);
	N2 = normalize(mat3x3(itWorldView) * N2);

	vec4 d1 = texture2D(samplerDiffuse1, texCoordDetail);
	vec4 d2 = texture2D(samplerDiffuse2, texCoordDetail);
	vec4 d3 = texture2D(samplerDiffuse3, texCoordDetail);
	vec4 d4 = texture2D(samplerDiffuse4, texCoordDetail);
	
	vec4 diffuse = d1 * splat.x + d2 * splat.y + d3 * splat.z + d4 * splat.w;
	
	vec4 specular = diffuse;
	specular.w = normalSpecular.w;
	
	vec3 gamma = (2.2f).xxx;
	diffuse.xyz = pow(diffuse.xyz, gamma);
	specular.xyz = pow(specular.xyz, gamma);
	specular.xyz = vec3(0, 0, 0);
	
	oColor = vec4(diffuse.xyz, 1.0);
	oNormal = vec4(N2.xyz, 1.0);
	oSpecular = specular;
	oPosition = position;
	oEmissive = vec4(1, 1, 1, 1);
}
#endif