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

in vec2 texCoord;

out(vec4, oColor, 0);

sampler(2D, samplerDiffuse, DiffuseTexture);

void main()
{
	vec3 sceneColor = texture2D(samplerDiffuse, texCoord).xyz;
	sceneColor = sceneColor * vec3(0.9f, 0.85f, 0.75f);
	sceneColor = max(vec3(0, 0, 0), sceneColor - vec3(0.7f, 0.7f, 0.7f));
	
	oColor = vec4(sceneColor.xyz, 1.0f);
}
#endif