#ifdef VERTEX_SHADER

attrib(vec3, iPosition, Position);
attrib(vec2, iTexCoord, TexCoord);
attrib(vec4, iColor, Color);

uniform(mat4x4, modelViewProjection, ModelViewProjection);

out vec2 texCoord;
out vec4 color;

void main()
{
	texCoord = iTexCoord;
	color = iColor;
	gl_Position = modelViewProjection * vec4(iPosition, 1);
}

#else

in vec2 texCoord;
in vec4 color;

out(vec4, oColor, 0);

sampler(2D, samplerDiffuse, DiffuseTexture);

void main()
{
	vec4 diffuse = texture2D(samplerDiffuse, texCoord) * color;
	
#ifdef SRGB
	oColor.xyz = pow(diffuse.xyz, (1.0f / 2.2f).xxx);
	oColor.a = diffuse.a;
#else
	oColor = diffuse;
#endif
}
#endif