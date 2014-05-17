

vec3 get_diffuse(vec3 diffuseColor) {
	return diffuseColor;
}

float get_specular(vec3 normal, vec3 viewer, vec3 lightDir, float specularPower) {
	vec3 halfAngle = normalize(lightDir + viewer);
	
	float nDotH = saturate(dot(normal, halfAngle));
	float specularValue = pow(nDotH, specularPower);
	
	float base = 1.0 - dot(halfAngle, viewer);
	float exponential = pow(base, 5.0);
	
	float F0 = 1.333;
	float fresnel = exponential + F0 * (1.0 - exponential);
	
	specularValue *= fresnel;
	
	return specularValue;
}