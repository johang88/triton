// Package: lighting.phong

vec3 phong(vec3 normal, vec3 viewer, vec3 lightDir, float specularPower, 
vec3 lightColor, vec3 diffuseColor, vec3 specularColor, float att)
{
	vec3 halfAngle = normalize(lightDir + viewer);
	
	float nDotL = saturate(dot(normal, lightDir));
	float nDotH = saturate(dot(normal, halfAngle));
	
	float specularValue = pow(nDotH, specularPower);
	
	float base = 1.0 - dot(halfAngle, viewer);
	float exponential = pow(base, 5.0);
	
	float F0 = 1.333;
	float fresnel = exponential + F0 * (1.0 - exponential);
	
	specularValue *= fresnel;
	
	vec3 specular = specularColor * lightColor * specularValue;
	return ((nDotL * lightColor) * diffuseColor + specular) * att;
}