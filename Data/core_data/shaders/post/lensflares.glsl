import(/shaders/core);
#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_TEXCOORD_0) in vec2 iTexCoord;

out vec2 texCoord;

void main()
{
	texCoord = iTexCoord;
	gl_Position = vec4(iPosition, 1);
}

#else

noperspective in vec2 texCoord;

layout(location = 0) out vec4 oColor;

uniform sampler2D samplerScene;
uniform vec2 textureSize;

vec3 textureDistorted(sampler2D tex, vec2 texCoord, vec2 direction, vec3 distortion) {
	return vec3(
		texture(tex, texCoord + direction * distortion.x).x,
		texture(tex, texCoord + direction * distortion.y).y,
		texture(tex, texCoord + direction * distortion.z).z
	);
}

void main() {
	vec2 uv = -texCoord + vec2(1.0);
	
	vec2 texelSize = 1.0 / textureSize;
	vec3 distortion = vec3(-texelSize.x * 1.2, 0.9, texelSize.x * 1.1);
	vec3 direction = normalize(distortion);
	
	float ghostDispersal = 0.3;
	vec2 ghostVec = (vec2(0.5) - uv) * ghostDispersal;
	
	vec3 result = vec3(0);
	for (int i = 0; i < 8; i++) {
		vec2 offset = fract(uv + ghostVec * float(i));
		
		float weight = length(vec2(0.5) - offset) / length(vec2(0.5));
		weight = pow(1.0 - weight, 10.0);
		
		
		result += textureDistorted(samplerScene, offset, direction.xy, distortion).xyz * weight;
	}
	
	oColor = vec4(result, 1);
}
#endif