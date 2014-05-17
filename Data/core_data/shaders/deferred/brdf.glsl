
vec3 get_diffuse(vec3 diffuseColor) {
	return diffuseColor;
}

vec3 get_specular(vec3 normal, vec3 viewer, vec3 lightDir, float roughnessValue, vec3 specularColor) {
	// cook torrance specular brdf
	
	float refAtNormIncidence = 0.67;
	
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
	
	return rs;
}