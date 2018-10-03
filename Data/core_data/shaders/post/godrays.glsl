#include "/shaders/core"
#include "/shaders/post/postcommon"
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
in vec2 texCoord;

layout(location = 0) out vec4 oColor;

layout(binding = 0)uniform sampler2D samplerMask;
layout(location = 0) uniform vec3 sunPosition;
layout(location = 1) uniform vec2 texelSize;
layout(location = 2) uniform vec3 cameraDir;
layout(location = 3) uniform vec3 lightDir;

void main() {
	// vec3 sceneColor = texture(samplerScene, texCoord).xyz;
	/*//const float density = 0.1f;
	const int samples = 16;
	//const float weight = 0.8f;
	//const float decay = 1.04f;
	
	uv += uvOffset;
	
	float2 deltaTexCoord = (uv - lightPosition);
	
	deltaTexCoord *= 1.0f / samples * density;
	
	float col = tex2D(frame, uv).a;
	float illuminationDecay = 1.0f;
	
	for (int i = 0; i < samples; i++) {
		uv -= deltaTexCoord;
		float sample = tex2D(frame, uv).a;
		sample *= illuminationDecay * weight;
		col += sample;
		illuminationDecay *= decay;
	}
	
	float alpha = saturate(dot(cameraDirection, -lightDirection));
	
	color = godRaysColor * (col * intensity * alpha).x;*/
	
	const float density = 0.1;
	const int samples = 16;
	const float weight = 0.8;
	const float decay = 1.04;
	const float intensity = 0.02;
	
	vec2 uv = texCoord + texelSize;
	vec2 deltaUV = (uv - sunPosition.xy);
	deltaUV *= 1.0 / samples * density;
	
	vec3 acc = texture(samplerMask, uv).xyz;
	float illuminationDecay = 1.0;
	
	for (int i = 0; i < samples; i++) {
		uv -= deltaUV;
		
		vec3 maskSample = texture(samplerMask, uv).xyz;
		maskSample *= illuminationDecay * weight;
		
		acc += maskSample;
		illuminationDecay *= decay;
	}
	
	float alpha = saturate(dot(cameraDir, -lightDir));
	oColor.xyz = acc * (intensity * alpha);
	oColor.a = 1;
}
#endif