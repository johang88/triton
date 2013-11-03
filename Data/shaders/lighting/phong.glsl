// Package: lighting.phong

vec3 phong(vec3 normal, vec3 viewer, vec3 lightDir, float specularPower, 
vec3 lightColor, vec3 diffuseColor, vec3 specularColor, float att)
{
	vec3 halfAngle = normalize(lightDir + viewer);
	
	float nDotL = saturate(dot(normal, lightDir));
	float nDotH = saturate(dot(normal, halfAngle));
	
	float blinnPong = pow(nDotH, specularPower);
	float specularTerm = (specularPower + 2.0f) / (specularPower / 2.0f) * blinnPong;
	
	float base = saturate(dot(halfAngle, lightDir));
	float exponential = pow(1.0f - base, 5.0f);
	
	vec3 fresnelTerm = specularColor + (1.0f - specularColor) * exponential;
	
	vec3 specular = specularTerm * nDotL * fresnelTerm * lightColor;
	
	return (nDotL * lightColor * att) * diffuseColor + specular * att;
}