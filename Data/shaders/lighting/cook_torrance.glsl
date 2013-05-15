vec2 cook_torrance(vec3 normal, vec3 viewer, vec3 light, float roughnessValue, float refAtNormIncidence)
{
	vec3 halfVector = normalize(light + viewer);
	float nDotL = saturate(dot(normal, light));
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
	
	float fresnel = pow(1.0f - vDotH, 5.0f);
	fresnel *= 1.0f - refAtNormIncidence;
	fresnel += refAtNormIncidence;
	
	float rsNumerator = fresnel * geo * roughness;
	float rsDenominator = nDotV * nDotL;
	float rs = rsNumerator / rsDenominator;
	
	return vec2(max(0.0f, nDotL), max(0.0f, rs));
}