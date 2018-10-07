#include "/shaders/core"

#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_TEXCOORD_0) in vec2 iTexCoord;

out vec2 texCoord;

uniform mat4x4 modelViewProjection;

void main()
{
	texCoord = iTexCoord;
	
#if defined(SPOT_LIGHT) || defined(POINT_LIGHT)
	gl_Position = modelViewProjection * vec4(iPosition, 1);
#else
	gl_Position = vec4(iPosition, 1);
#endif
}

#else

#include "/shaders/brdf"

in vec2 texCoord;

layout(location = 0) out vec4 oColor;

uniform sampler2D samplerGBuffer0;
uniform sampler2D samplerGBuffer1;
uniform sampler2D samplerGBuffer2;
uniform sampler2D samplerDepth;

uniform vec2 screenSize;
uniform vec3 lightColor;
uniform vec3 cameraPosition;
uniform vec3 lightDirection;
uniform sampler2D samplerShadow;

void main()
{
	vec2 project = gl_FragCoord.xy / screenSize.xy;
	
	vec4 gbuffer0 = texture(samplerGBuffer0, project); // color
	vec4 gbuffer1 = texture(samplerGBuffer1, project); // normal
	vec4 gbuffer2 = texture(samplerGBuffer2, project); // specular stuff
	
	if (gbuffer1.w == 0)
		discard;
	
	float depth = texture(samplerDepth, project).x;
	vec3 position = decodeWorldPosition(project, depth);
	
	vec3 lightDir = -normalize(lightDirection);
	vec3 lightVec = lightDir;
	
	float attenuation = 1.0;

	vec3 normal = decodeNormals(gbuffer1.xyz);
	
	float nDotL = saturate(dot(normal, lightDir));
	vec3 lighting = vec3(0, 0, 0);

	float attenuationNdotL = attenuation * nDotL;
	
	if (attenuationNdotL > 0) {
#ifdef SHADOWS
		// Sample shadow buffer
		float shadow = texture(samplerShadow, project).x;
		attenuationNdotL *= shadow;
#endif

		float metallic = gbuffer2.x;
		float roughness = gbuffer2.y;
		float specular = gbuffer2.z;
		
		vec3 diffuse = decodeDiffuse(gbuffer0.xyz);
		
		vec3 eyeDir = normalize(cameraPosition - position);

		vec3 F0 = vec3(0.08);
		F0 = mix(F0, diffuse, metallic);
		
		lighting = brdf(normal, eyeDir, lightDir, roughness, metallic, lightColor * attenuationNdotL, diffuse, F0);
	}

	oColor.xyz = lighting.xyz;
	oColor.w = 1.0;
}
#endif