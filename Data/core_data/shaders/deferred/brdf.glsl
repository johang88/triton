
vec3 get_diffuse(vec3 diffuseColor) {
	return diffuseColor;
}

vec3 cook_torrance(vec3 normal, vec3 viewer, vec3 lightDir, float roughnessValue, vec3 specularColor, float energy) {
	// cook torrance specular brdf
	
	float refAtNormIncidence = 1.33;
	
	vec3 halfVector = normalize(lightDir + viewer);
	float nDotL = saturate(dot(normal, lightDir));
	float nDotH = saturate(dot(normal, halfVector));
	float nDotV = saturate(dot(normal, viewer));
	float vDotH = saturate(dot(viewer, halfVector));
	float rSq = roughnessValue * roughnessValue;
	
	float geoNumerator = 2.0f * nDotH;
	float geoDenominator = vDotH;
	
	float geoB = (geoNumerator * nDotV) / geoDenominator;
	float geoC = (geoNumerator * nDotL) / geoDenominator;
	float geo = min(1.0f, min(geoB, geoC));
	
	float roughnessA = 1.0f / (4.0f * rSq * pow(nDotH, 4));
	float roughnessB = nDotH * nDotH - 1.0f;
	float roughnessC = rSq * nDotH * nDotH;
	
	float roughness = roughnessA * exp(roughnessB / roughnessC);
	
	/*float fresnel = pow(1.0f - vDotH, 5.0f);
	fresnel *= 1.0f - refAtNormIncidence;
	fresnel += refAtNormIncidence;*/
	
	vec3 fresnel = specularColor + (saturate(50.0 * specularColor.y) - specularColor) * exp2((-5.55473 * vDotH - 6.98316) * vDotH);
	
	vec3 rsNumerator = fresnel * geo * roughness;
	float rsDenominator = nDotV * nDotL;
	vec3 rs = rsNumerator / rsDenominator;
	
	return rs * specularColor;
	//return (energy * geo * roughness) * fresnel;
}

vec3 brdf_stuff(vec3 normal, vec3 viewer, vec3 lightDir, float roughnessValue, vec3 specularColor, float energy) {
	vec3 halfAngle = normalize(lightDir + viewer);
	
	float nDotL = saturate(dot(normal, lightDir));
	float nDotH = saturate(dot(normal, halfAngle));
	
	float specularPower = 128 * (1.0 - roughnessValue);
	float specularValue = pow(nDotH, specularPower);
	
	float base = 1.0 - dot(halfAngle, viewer);
	float exponential = pow(base, 5.0);
	
	float F0 = 1.333;
	float fresnel = exponential + F0 * (1.0 - exponential);
	
	specularValue *= fresnel;
	
	vec3 specular = specularColor * specularValue;
	
	return specular;
}

vec3 brdf_stuff2(vec3 normal, vec3 viewer, vec3 lightDir, float roughness, vec3 specularColor, float energy) {
	vec3 halfVector = normalize(lightDir + viewer);
	float nDotL = saturate(dot(normal, lightDir));
	float nDotH = saturate(dot(normal, halfVector));
	float nDotV = saturate(dot(normal, viewer));
	float vDotH = saturate(dot(viewer, halfVector));
	
	float m = roughness * roughness;
	float m2 = m * m;
	float d = (nDotH * m2 - nDotH) * nDotH + 1;
	float D = m2 / (d * d);
	
	float k = square(roughness) * 0.5;
	float schlickV = nDotV * (1 - k) + k;
	float schlickL = nDotL * (1 - k) + k;
	float G = 0.25 / (schlickV * schlickL);
	
	vec3 F = specularColor + (saturate(50.0 * specularColor.y) - specularColor) * exp2((-5.55473 * vDotH - 6.98316) * vDotH);
	
	return(D * G) * F;
}

vec3 get_specular(vec3 normal, vec3 viewer, vec3 lightVec, float roughness, vec3 specularColor, float radius) {
	float a = roughness * roughness;
	
	float energy = 1;
	
	vec3 R = reflect(-viewer, normal);
	float rlengthl = inversesqrt(dot(lightVec, lightVec));
	
	float angle = saturate(radius * rlengthl);
	energy *= square(a / saturate(a + 0.5 * angle));
	
	vec3 closestPointOnRay = dot(lightVec, R) * R;
	vec3 centerToRay = closestPointOnRay - lightVec;
	vec3 closesPointOnSphere = lightVec + centerToRay * saturate(radius * inversesqrt(dot(centerToRay, centerToRay)));
	// lightVec = closesPointOnSphere;
	
	lightVec = normalize(lightVec);
	
	return cook_torrance(normal, viewer, lightVec, roughness, specularColor, energy);
}