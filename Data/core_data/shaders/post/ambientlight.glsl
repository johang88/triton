#include "/shaders/core"
#include "/shaders/post/postcommon"
#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_TEXCOORD_0) in vec2 iTexCoord;

out vec2 texCoord;

void main()
{
	texCoord = iTexCoord;
	gl_Position = vec4(iPosition, 1);
}

#else
#include "/shaders/brdf"

in vec2 texCoord;

layout(location = 0) out vec4 oColor;

uniform sampler2D samplerGBuffer0;
uniform sampler2D samplerGBuffer1;
uniform sampler2D samplerGBuffer2;
uniform sampler2D samplerDepth;
uniform sampler2D samplerScene;
uniform samplerCube samplerDiffuseEnv;
uniform samplerCube samplerSpecularEnv;

uniform mat4x4 viewMatrix;

uniform vec3 cameraPosition;

void main() {
	vec4 gbuffer0 = texture(samplerGBuffer0, texCoord);
	vec4 gbuffer1 = texture(samplerGBuffer1, texCoord);
	vec4 gbuffer2 = texture(samplerGBuffer2, texCoord);
	
	vec3 diffuse = decodeDiffuse(gbuffer0.xyz);
	vec3 sceneColor = texture(samplerScene, texCoord).xyz;
	
	if (gbuffer1.w == 0) {
		oColor = vec4(sceneColor.xyz, 1.0);
	} else {
		float depth = texture(samplerDepth, texCoord).x;
		vec3 position = decodeWorldPosition(texCoord, depth);
		vec3 normal = decodeNormals(gbuffer1.xyz);
		
		float metallic = gbuffer2.x;
		float roughness = gbuffer2.y;
		float specular = gbuffer2.z;
		float subsurfaceLightingAmount = gbuffer2.w;
		
		vec3 V = normalize(cameraPosition - position);
		vec3 R = normalize(reflect(-V, normal));
		
		float vDotN = saturate(dot(V, normal));
		
		vec3 diffuseIBL = textureLod(samplerDiffuseEnv, normal, 0).xyz * 2.0;
		
		float specMipLevel = clamp(roughness * 9, 0.0, 9.0);
		vec3 specularIBL = textureLod(samplerSpecularEnv, R, specMipLevel).xyz * 2.0;
		
		vec3 res = environment_brdf(vDotN, roughness, metallic, specular, diffuse, diffuseIBL, specularIBL, normal);
		
		float ao = 1;
		
		res += sceneColor;
		
		if (subsurfaceLightingAmount > 0.0) {
			float viewDependantSplit = 0.5;
			vec3 subsurfaceLighting = diffuseIBL * viewDependantSplit;
			subsurfaceLighting += textureLod(samplerDiffuseEnv, -V, 0).xyz * ao * (1.0 - viewDependantSplit);

			res += subsurfaceLighting * diffuse * subsurfaceLightingAmount; 
		}
		
		oColor = vec4(res, 1.0);
	}
}
#endif