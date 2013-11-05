#ifdef VERTEX_SHADER

attrib(vec3, iPosition, Position);
attrib(vec2, iTexCoord, TexCoord);

out vec2 texCoord;

uniform(mat4x4, modelViewProjection, ModelViewProjection);

void main()
{
	texCoord = iTexCoord;
	
	gl_Position = modelViewProjection * vec4(iPosition, 1);
}

#else

import(shaders/utility/utils);
import(shaders/lighting/phong);

in vec2 texCoord;

out(vec4, oColor, 0);

sampler(2D, samplerNormal, NormalTexture);
sampler(2D, samplerPosition, PositionTexture);
sampler(2D, samplerSpecular, SpecularTexture);
sampler(2D, samplerDiffuse, DiffuseTexture);
sampler(2D, samplerShadow, ShadowMap);

uniform(vec3, lightPosition, LightPosition);
uniform(vec3, lightColor, LightColor);
uniform(vec2, spotParams, SpotLightParams);
uniform(float, lightRange, LightRange);
uniform(vec2, screenSize, ScreenSize);
uniform(vec3, cameraPosition, CameraPosition);
uniform(vec3, lightDirection, LightDirection);
#ifdef SHADOWS
uniform(mat4x4, invView, InverseViewMatrix);
uniform(mat4x4, shadowViewProj, ShadowViewProjection);
uniform(float, inverseShadowMapSize, InverseShadowMapSize);
uniform(vec2, clipPlane, ClipPlane);
uniform(float, shadowBias, ShadowBias);

const int SAMPLES = 4;

vec2 poissonDisk[16] = vec2[](
	vec2( -0.94201624, -0.39906216 ),
	vec2( 0.94558609, -0.76890725 ),
	vec2( -0.094184101, -0.92938870 ),
	vec2( 0.34495938, 0.29387760 ),
	vec2( -0.91588581, 0.45771432 ),
	vec2( -0.81544232, -0.87912464 ),
	vec2( -0.38277543, 0.27676845 ),
	vec2( 0.97484398, 0.75648379 ),
	vec2( 0.44323325, -0.97511554 ),
	vec2( 0.53742981, -0.47373420 ),
	vec2( -0.26496911, -0.41893023 ),
	vec2( 0.79197514, 0.19090188 ),
	vec2( -0.24188840, 0.99706507 ),
	vec2( -0.81409955, 0.91437590 ),
	vec2( 0.19984126, 0.78641367 ),
	vec2( 0.14383161, -0.14100790 )
);

float random(vec3 seed, int i){
	vec4 seed4 = vec4(seed,i);
	float dot_product = dot(seed4, vec4(12.9898, 78.233, 45.164, 94.673));
	return fract(sin(dot_product) * 43758.5453);
}

float check_shadow(sampler2D shadowMap, vec3 viewPos, mat4x4 invView, mat4x4 shadowViewProj, float texelSize) {
	vec3 worldPos = (invView * vec4(viewPos, 1)).xyz;
	vec4 shadowUv = shadowViewProj * vec4(worldPos, 1);

	vec2 jitterFactor = fract(gl_FragCoord.xy * vec2(18428.4f, 23614.3f)) * 2.0f - 1.0f;
	vec2 o = texelSize.xx * 0.93f * jitterFactor;

	vec2 uv = 0.5f * shadowUv.xy / shadowUv.w + vec2(0.5f, 0.5f);
	
	float distance = (shadowUv.z / (clipPlane.y - clipPlane.x)) - shadowBias;
	
	float c = 0.0f;
	for (int i = 0; i < SAMPLES; i++) {
		int index = int(16.0 * random(gl_FragCoord.xyy, i)) % 16;
		c += step(distance, texture2D(shadowMap, uv.xy - poissonDisk[index] / 700.0f).x);
	}
	
	return c / SAMPLES;
}
#endif

void main()
{
	vec2 project = gl_FragCoord.xy / screenSize.xy;
	
	vec3 normal = normalize(texture2D(samplerNormal, project).xyz);
	vec4 specularColor = texture2D(samplerSpecular, project);
	vec3 diffuse = texture2D(samplerDiffuse, project).xyz;
	vec3 position = texture2D(samplerPosition, project).xyz;
	
	float specularPower = 32 * specularColor.w;
	
	vec3 lightDir = lightPosition - position;
	float dist = length(lightDir);
	lightDir = lightDir / dist;
	
	vec3 eyeDir = normalize(cameraPosition - position);
	
	float attenuation = dist / lightRange;
	attenuation = saturate(1.0f - (attenuation * attenuation));
	
	float spotLightAngle = saturate(dot(-lightDirection, lightDir));
	float spotFallof = 1.0f - saturate((spotLightAngle - spotParams.x) / (spotParams.y - spotParams.x));
	
#ifdef SHADOWS
	float shadow = check_shadow(samplerShadow, position, invView, shadowViewProj, inverseShadowMapSize);
#else
	float shadow = 1.0f;
#endif
	
	oColor.xyz = phong(normal, eyeDir, lightDir, specularPower, lightColor, diffuse, specularColor.xyz, attenuation * spotFallof) * shadow;
	
	oColor.w = 1.0f;
}
#endif