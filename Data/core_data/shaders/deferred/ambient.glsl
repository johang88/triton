#include "/shaders/core"
#include "/shaders/brdf"

#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_TEXCOORD_0) in vec2 iTexCoord;

out vec2 texCoord;
out vec3 position;

uniform mat4x4 modelViewProjection;

void main()
{
	texCoord = iTexCoord;
	
	gl_Position = modelViewProjection * vec4(iPosition, 1);
	position = gl_Position.xyz;
}

#else
in vec2 texCoord;
in vec3 position;

layout(location = 0) out vec4 oColor;

uniform sampler2D samplerGBuffer0;
uniform sampler2D samplerGBuffer1;
uniform sampler2D samplerGBuffer2;
uniform sampler2D samplerDepth;
uniform sampler2D samplerSpecularIntegration;
uniform samplerCube samplerIrradiance;
uniform samplerCube samplerSpecular;
uniform vec3 ambientColor;

uniform int mode;
uniform float irradianceStrength;
uniform float specularStrength;

uniform vec3 cameraPosition;

void main()
{
	vec4 gbuffer0 = texture2D(samplerGBuffer0, texCoord);
	vec4 gbuffer1 = texture2D(samplerGBuffer1, texCoord);
	vec4 gbuffer2 = texture2D(samplerGBuffer2, texCoord);
	
	vec3 diffuse = decodeDiffuse(gbuffer0.xyz);
	vec3 N = decodeNormals(gbuffer1.xyz);
	
	float metallic = gbuffer2.x;
	float roughness = gbuffer2.y;

	float depth = texture(samplerDepth, texCoord).x;
	vec3 position = decodeWorldPosition(texCoord, depth);
	
	if (gbuffer1.w == 0) {
		if (mode == 1) {
			vec3 sky = textureLod(samplerSpecular, position, 0).xyz;
			oColor = vec4(sky, 1.0);
		} else {
			oColor = vec4(diffuse, 1.0);
		}
	} else {
		vec3 lighting = vec3(0);

		if (mode == 1) {
			vec3 V = normalize(cameraPosition - position);
			vec3 R = normalize(reflect(-V, N));

			const float MAX_REFLECTION_LOD = 9.0;
			float mipLevel = MAX_REFLECTION_LOD * roughness;

			vec3 F0 = vec3(0.04);
			F0 = mix(F0, diffuse, metallic);

			vec3 F = f_schlick_roughness(max(dot(N, V), 0.0), F0, roughness);

			vec3 kS = F;
			vec3 kD = vec3(1.0) - kS;
			kD *= 1.0 - metallic;

			vec3 specularIBL = textureLod(samplerSpecular, R, mipLevel).xyz * specularStrength;
			vec2 brdf = texture(samplerSpecularIntegration, vec2(max(dot(N, V), 0.0), 1.0 - roughness)).rg;
			vec3 specular = specularIBL * (F * brdf.x + brdf.y);

			vec3 irradianceIBL = textureLod(samplerIrradiance, N, 0).xyz * irradianceStrength;
			lighting = (kD * irradianceIBL * diffuse) + specular;
		} else {
			lighting = ambientColor * diffuse;
		}

		oColor = vec4(lighting, 1.0);


	}
}
#endif