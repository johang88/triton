#ifdef VERTEX_SHADER

attrib(vec3, iPosition, Position);
attrib(vec2, iTexCoord, TexCoord);

out vec2 texCoord;

uniform(mat4x4, modelViewProjection, ModelViewProjection);

void main()
{
	texCoord = iTexCoord;
	
	gl_Position = modelViewProjection * vec4(iPosition, 1);
}

#else

import(shaders/utility/utils);

in vec2 texCoord;

out(vec4, oColor, 0);
out(vec4, oSpecular, 1);

sampler(2D, samplerNormal, NormalTexture);

uniform(vec3, ambientColor, AmbientColor);
uniform(vec2, screenSize, ScreenSize);

void main()
{
	vec2 project = gl_FragCoord.xy / screenSize.xy;
	
	vec3 normal = normalize(texture2D(samplerNormal, texCoord).xyz);
	
	oColor = vec4(mix(ambientColor * 0.5f, ambientColor, 1.0f - saturate(normal.z * 0.5f + 0.5f)), 1.0f);
	oSpecular = vec4(0, 0, 0, 0);
}
#endif