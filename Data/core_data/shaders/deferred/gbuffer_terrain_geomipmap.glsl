#include "/shaders/core"

uniform mat4x4 itWorld;
uniform vec3 cameraPosition;

uniform mat4x4 world;
uniform mat4x4 worldView;
uniform mat4x4 modelViewProjection;

uniform sampler2D samplerHeightMap;
uniform vec4 uTerrainParameters;
uniform vec4 uTerrainParameters2;

float maxComponent(vec2 v) {
	return max(v.x, v.y);
}

vec2 roundToIncrement(vec2 value, float increment) {
    return round(value * (1.0 / increment)) * increment;
}

#ifdef VERTEX_SHADER
layout(location = ATTRIB_POSITION) in vec3 iPosition;

out vec3 normal;
out vec3 tangent;
out vec3 bitangent;
out vec2 texCoord;
out vec4 position;

void main() {
    normal = vec3(0, 1, 0);
	tangent = vec3(0, 0, -1);
	bitangent = normalize(cross(normal, tangent));

    const float metersPerHeightfieldTexel = uTerrainParameters.x;
	const float heightfieldTexelsPerMeter = uTerrainParameters.y;
	const float invBaseGridSizeTexels = uTerrainParameters.z;
	const float maxHeight = uTerrainParameters.w;

    const vec2 heightfieldInvSize = vec2(uTerrainParameters2.x, uTerrainParameters2.y);

    float gridLevel = iPosition.y;
    float mipMetersPerHeightfieldTexel = metersPerHeightfieldTexel * exp2(gridLevel);

    vec2 objectToWorld = roundToIncrement(cameraPosition.xz, mipMetersPerHeightfieldTexel);

    position.xyz = vec3(iPosition.x * metersPerHeightfieldTexel + objectToWorld.x, 0.0, iPosition.z * metersPerHeightfieldTexel + objectToWorld.y);
	position.w = 1;

    vec3 positionOS = position.xyz - cameraPosition.xyz;

    float size = max(0.5, maxComponent(abs(positionOS.xz * 2.0 * invBaseGridSizeTexels)));

    float gridLOD = max(log2(size) - 0.75, 0.0);

    float lowMIP = floor(gridLOD);
    float highMIP = lowMIP + 1.0;

    float fractionalLevel = gridLOD - lowMIP;

    float highMIPHalfTexelOffset = exp2(lowMIP);
    float lowMIPHalfTexelOffset = highMIPHalfTexelOffset * 0.5;

    vec2 lowMIPTexCoord  = (position.xz * heightfieldTexelsPerMeter + lowMIPHalfTexelOffset)  * heightfieldInvSize.xy;
	vec2 highMIPTexCoord = (position.xz * heightfieldTexelsPerMeter + highMIPHalfTexelOffset) * heightfieldInvSize.xy;

    float lowMIPValue;

    if (lowMIP > 0) {
        lowMIPValue = textureLod(samplerHeightMap, lowMIPTexCoord, lowMIP).x;
    } else {
        const float smoothness = 0.35;
        lowMIPValue  = 
         (textureLod(samplerHeightMap, vec2( heightfieldInvSize.x,  heightfieldInvSize.y) * smoothness + lowMIPTexCoord, lowMIP).x +
          textureLod(samplerHeightMap, vec2( heightfieldInvSize.x, -heightfieldInvSize.y) * smoothness + lowMIPTexCoord, lowMIP).x +
          textureLod(samplerHeightMap, vec2( heightfieldInvSize.x, -heightfieldInvSize.y) * smoothness + lowMIPTexCoord, lowMIP).x +
          textureLod(samplerHeightMap, vec2(-heightfieldInvSize.x,  heightfieldInvSize.y) * smoothness + lowMIPTexCoord, lowMIP).x) * 0.25;
    }
	
	float highMIPValue = textureLod(samplerHeightMap, highMIPTexCoord, highMIP).x;
	position.y = mix(lowMIPValue, highMIPValue, fractionalLevel) * maxHeight;
	
	if (max(abs(lowMIPTexCoord.x - 0.5), abs(lowMIPTexCoord.y - 0.5)) > 0.5) {
        position.y = -100;
    }
	
	gl_Position = modelViewProjection * vec4(position.xyz, 1);
	
    texCoord = (position.xz * heightfieldTexelsPerMeter + 0.5)  * heightfieldInvSize.xy;
}

#else

#include "/shaders/brdf"

in vec3 normal;
in vec3 tangent;
in vec3 bitangent;
in vec2 texCoord;
in vec4 position;

layout(location = 0) out vec4 oColor;
layout(location = 1) out vec4 oNormal;
layout(location = 2) out vec4 oSpecular;

uniform sampler2D samplerSplatMap;
uniform sampler2D samplerDiffuse0;
uniform sampler2D samplerDiffuse1;
uniform sampler2D samplerDiffuse2;
uniform sampler2D samplerDiffuse3;
uniform sampler2D samplerNormal0;
uniform sampler2D samplerNormal1;
uniform sampler2D samplerNormal2;
uniform sampler2D samplerNormal3;
uniform sampler2D samplerNormalMap;

uniform vec4 uDetailMapScale;

void get_material(out vec3 diffuse, out vec3 normals, out float metallic, out float specular, out float roughness, out float occlusion) {
    const float metersPerHeightfieldTexel = uTerrainParameters.x;
	const float heightfieldTexelsPerMeter = uTerrainParameters.y;
	const float invBaseGridSizeTexels = uTerrainParameters.z;
	const float maxHeight = uTerrainParameters.w;
	
	const vec2 heightfieldInvSize = vec2(uTerrainParameters2.x, uTerrainParameters2.y);
	
	vec2 texCoord = (position.xz * heightfieldTexelsPerMeter + 0.5)  * heightfieldInvSize.xy;;
	
	float r = position.y / 512.0;
	
	vec4 splat = textureLod(samplerSplatMap, texCoord, 0);

	vec3 dh0 = texture(samplerDiffuse0, texCoord * uDetailMapScale.x).xyz;
	vec3 dh1 = texture(samplerDiffuse1, texCoord * uDetailMapScale.y).xyz;
	vec3 dh2 = texture(samplerDiffuse2, texCoord * uDetailMapScale.z).xyz;
	vec3 dh3 = texture(samplerDiffuse3, texCoord * uDetailMapScale.w).xyz;
	
	vec4 nr0 = texture(samplerNormal0, texCoord * uDetailMapScale.x);
	vec4 nr1 = texture(samplerNormal1, texCoord * uDetailMapScale.y);
	vec4 nr2 = texture(samplerNormal2, texCoord * uDetailMapScale.z);
	vec4 nr3 = texture(samplerNormal3, texCoord * uDetailMapScale.w);
	
	vec3 dh = dh0 * splat.x + dh1 * splat.y + dh2 * splat.z + dh3 * splat.w;
	vec4 nr = nr0 * splat.x + nr1 * splat.y + nr2 * splat.z + nr3 * splat.w;
	
	diffuse = pow(dh, vec3(2.2));
	
	mat3x3 TBN = mat3x3(normalize(tangent), normalize(bitangent), normalize(normal));
	vec3 N = normalize(TBN * normalize(texture(samplerNormalMap, texCoord).xyz * 2.0 - 1.0));

	TBN = mat3x3(normalize(tangent), normalize(bitangent), normalize(N));
	normals = normalize(TBN * normalize(nr.xyz * 2.0 - 1.0));

    metallic = 0;
    roughness = 1;
    occlusion = 1;
	specular = 0.5;
}

void main() {
	vec3 diffuse;
	vec3 normals;
	float metallic, specular, roughness, occlusion;

	get_material(diffuse, normals, metallic, specular, roughness, occlusion);
	roughness = max(0.01, roughness);

	oColor = vec4(encodeDiffuse(diffuse), 0);
	oNormal = vec4(encodeNormals(normals), 1);
	oSpecular = vec4(metallic, roughness, specular, occlusion);
}
#endif