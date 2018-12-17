//#define OREN_NAYAR

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

vec3 f_schlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
} 

float d_ggx(vec3 N, vec3 H, float roughness)
{
    float a      = roughness*roughness;
    float a2     = a*a;
    float NdotH  = max(dot(N, H), 0.0);
    float NdotH2 = NdotH*NdotH;
	
    float num   = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = PI * denom * denom;
	
    return num / denom;
}

float g_schlickggx(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r*r) / 8.0;

    float num   = NdotV;
    float denom = NdotV * (1.0 - k) + k;
	
    return num / denom;
}
float g_smith(vec3 N, vec3 V, vec3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2  = g_schlickggx(NdotV, roughness);
    float ggx1  = g_schlickggx(NdotL, roughness);
	
    return ggx1 * ggx2;
}

vec3 f_schlick_roughness(float cosTheta, vec3 F0, float roughness) {
    return F0 + (max(vec3(1.0 - roughness), F0) - F0) * pow(1.0 - cosTheta, 5.0);
} 

vec3 brdf(vec3 N, vec3 V, vec3 L, float roughness, float metallic, vec3 radiance, vec3 albedo, vec3 F0) {
	vec3 H = normalize(L + V);

	float NDF = d_ggx(N, H, roughness);
	float G = g_smith(N, V, L, roughness);
	vec3 F = f_schlick(max(dot(H, V), 0.0), F0);

	vec3 numerator = NDF * G * F;
	float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0);
	vec3 specular = numerator / max(denominator, 0.001);

	vec3 kS = F;
	vec3 kD = vec3(1.0) - kS;

	kD *= 1.0 - metallic;
	return (kD * albedo / PI + specular) * radiance;
}
