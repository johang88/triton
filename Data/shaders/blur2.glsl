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

uniform(float, texelSize, TexelSize);
sampler(2D, samplerDiffuse, DiffuseTexture);

void main()
{
	vec4 sum = texture2D(samplerDiffuse, texCoord);
	
	sum += texture2D(samplerDiffuse, vec2(texCoord.x, texCoord.y - 4.0*texelSize)) * 0.05;
	sum += texture2D(samplerDiffuse, vec2(texCoord.x, texCoord.y - 3.0*texelSize)) * 0.09;
	sum += texture2D(samplerDiffuse, vec2(texCoord.x, texCoord.y - 2.0*texelSize)) * 0.12;
	sum += texture2D(samplerDiffuse, vec2(texCoord.x, texCoord.y - texelSize)) * 0.15;
	sum += texture2D(samplerDiffuse, vec2(texCoord.x, texCoord.y)) * 0.16;
	sum += texture2D(samplerDiffuse, vec2(texCoord.x, texCoord.y + texelSize)) * 0.15;
	sum += texture2D(samplerDiffuse, vec2(texCoord.x, texCoord.y + 2.0*texelSize)) * 0.12;
	sum += texture2D(samplerDiffuse, vec2(texCoord.x, texCoord.y + 3.0*texelSize)) * 0.09;
	sum += texture2D(samplerDiffuse, vec2(texCoord.x, texCoord.y + 4.0*texelSize)) * 0.05;
	
	oColor = vec4(sum.xyz, 1.0f);
}
#endif