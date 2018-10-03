#define OREN_NAYAR

vec3 get_diffuse(vec3 diffuseColor, vec3 normal, vec3 viewer, vec3 light, float roughness) {
#ifdef OREN_NAYAR
	float gamma = dot(viewer - normal * dot(viewer, normal), light - normal * dot(light, normal));
	
	float roughSq = roughness * roughness;
	
	float A = 1.0f - 0.5f * (roughSq / (roughSq + 0.57f));
	float B = 0.45f * (roughSq / (roughSq + 0.09f));
	
	float alpha = max(acos(dot(viewer, normal)), acos(dot(light, normal)));
	float beta = min(acos(dot(viewer, normal)), acos(dot(light, normal)));
	
	float C = sin(alpha) * tan(beta);
	
	float final = A + B * max(0.0f, gamma) * C;
	
	return diffuseColor * max(final, 0.0);
#else
	return diffuseColor / PI;
#endif
}

float d_ggx(float roughness, float nDotH) {
	float m = roughness * roughness;
	float m2 = m * m;
	float d = (nDotH * m2 - nDotH) * nDotH + 1;
	return (m2 / (PI * d * d));
}

float g_schlick(float roughness, float nDotV, float nDotL) {
	float k = square(roughness) * 0.5;
	float schlickV = nDotV * (1 - k) + k;
	float schlickL = nDotL * (1 - k) + k;
	return 0.25 / (schlickV * schlickL);
}

vec3 f_schlick(vec3 specularColor, float vDotH) {
	float fc = exp2(-5.55473 * vDotH - 6.98316) * vDotH;
	return saturate(50.0 * specularColor.y) * fc + (1.0 - fc) * specularColor;
}

vec3 brdf_stuff2(vec3 normal, vec3 viewer, vec3 lightDir, float roughness, vec3 specularColor) {
	vec3 H = normalize(lightDir + viewer);
	float nDotL = saturate(dot(normal, lightDir));
	float nDotH = saturate(dot(normal, H));
	float nDotV = max(0.001, saturate(dot(normal, viewer)));
	float vDotH = saturate(dot(viewer, H));
	
	float D = d_ggx(roughness, nDotH) / PI;
	float G = g_schlick(roughness, nDotV, nDotL);
	
	vec3 F = f_schlick(specularColor, vDotH);
	
	return D * G * F;
}

vec3 get_specular(vec3 normal, vec3 viewer, vec3 lightDir, float roughness, vec3 specularColor) {
	return brdf_stuff2(normal, viewer, lightDir, roughness, specularColor);
}