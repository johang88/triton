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

sampler(2D, samplerDiffuse, DiffuseTexture);

uniform(vec3, ambientColor, AmbientColor);
uniform(vec2, screenSize, ScreenSize);

void main()
{
	vec2 project = gl_FragCoord.xy / screenSize.xy;
	
	vec3 diffuse = texture2D(samplerDiffuse, texCoord).xyz;

	oColor = vec4(diffuse * ambientColor, 1.0f);
}
#endif