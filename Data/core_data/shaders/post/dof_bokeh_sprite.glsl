#include "/shaders/core"
#include "/shaders/post/postcommon"

uniform vec4 screenSize;
uniform sampler2D samplerColorCoc;

#ifdef VERTEX_SHADER

layout(location = ATTRIB_POSITION) in vec3 iPosition;
layout(location = ATTRIB_TEXCOORD_0) in vec2 iTexCoord;

out vec2 texCoord;
out vec4 colorAlpha;

void main()
{
	// 0 = (0, 0)
	// 1 = (0, 1)
	// 2 = (1, 1)
	// 3 = (1, 0)
	// 0, 2, 1
	// 0, 3, 2
	
	
	int quadIndex = gl_VertexID / 6;
	int vertexIndex = gl_VertexID % 4;
	
	vec4 colorAndDepth = textureLod(samplerColorCoc, iTexCoord, 0);

	vec2 position2d = vec2(
		(vertexIndex < 2) ? 0.0 : 1.0,
		(vertexIndex > 0 && vertexIndex < 3) ? 1.0 : 0.0
	);
	
	texCoord = position2d;
	
	float near = colorAndDepth.w < 0.0 ? 0.0 : 1.0;
	float cocScale = abs(colorAndDepth.w);
	float size = min(cocScale, 32.0);
	
	// Apply scale
	position2d -= vec2(0.5);
	position2d *= size;
	position2d += vec2(0.5);
	
	// Go to "pixel" space
	position2d += iTexCoord * screenSize.xy;
	
	// Back to texture space (0 .. 1)
	position2d *= screenSize.zw;
	
	// And to clip space thing
	position2d = vec2(-1.0) + position2d * vec2(1, 2);
	position2d.x += near;
	
	gl_Position.xy = position2d;
	gl_Position.z = 0.0;
	gl_Position.w = cocScale < 1.0 ? -1.0 : 1.0;
	
	colorAlpha = vec4(colorAndDepth.xyz, 1.0 * rcp(size * size));
}

#else

in vec2 texCoord;
in vec4 colorAlpha;

layout(location = 0) out vec4 oColor;

uniform sampler2D samplerBokehSprite;

void main() {
	vec3 sprite = texture(samplerBokehSprite, texCoord).xyz;
	float luminance = calc_luminance(sprite);
	
	oColor = vec4(sprite.xyz * colorAlpha.xyz, luminance);
}
#endif