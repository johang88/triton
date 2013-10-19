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

in vec2 texCoord;

out(vec4, oColor, 0);
out(vec4, oSpecular, 1);

sampler(2D, samplerNormal, NormalTexture);
sampler(2D, samplerPosition, PositionTexture);
sampler(2D, samplerSpecular, SpecularTexture);
sampler(2D, samplerDiffuse, DiffuseTexture);

uniform(vec3, lightDirection, LightDirection);
uniform(vec3, lightColor, LightColor);
uniform(vec2, screenSize, ScreenSize);
uniform(vec3, cameraPosition, CameraPosition);

void main()
{
	vec2 project = gl_FragCoord.xy / screenSize.xy;
	
	vec3 normal = normalize(texture2D(samplerNormal, project).xyz);
	vec3 position = texture2D(samplerPosition, project).xyz;
	
	vec3 lightDir = -normalize(lightDirection);
	
	float nDotL = saturate(dot(normal, lightDir));
	
	vec3 eyeDir = normalize(cameraPosition - position);
	vec3 H = normalize(eyeDir + lightDir);
	
	vec4 specularColor = texture2D(samplerSpecular, project);
	
	float nDotH = saturate(dot(normal, H));
	float specularPower = pow(nDotH, 16 * specularColor.w);
	
	vec3 specular = specularColor.xyz * lightColor.xyz * specularPower;
	
	vec3 diffuse = texture2D(samplerDiffuse, project);
	
	oColor.xyz = diffuse * (lightColor * nDotL) + specular * nDotL;
	oColor.a = 1.0f;
}
#endif