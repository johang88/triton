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

uniform(float, A, A);
uniform(float, B, B);
uniform(float, C, C);
uniform(float, D, D);
uniform(float, E, E);
uniform(float, F, F);
uniform(float, W, W);

vec3 tonemap(vec3 x)
{
	return ((x*(A*x+C*B)+D*E)/(x*(A*x+B)+D*F))-E/F;
}

void main()
{
	vec3 sceneColor = texture2D(samplerDiffuse, texCoord).xyz;
	
	float exposureBias = 2.0f;
	vec3 toneMappedScene = tonemap(sceneColor * exposureBias);
	
	vec3 whiteScale = 1.0f / tonemap(W.xxx);
	toneMappedScene *= whiteScale;
	
	vec3 retColor = pow(toneMappedScene, (1.0f / 2.2f).xxx);
	
	oColor = vec4(retColor.xyz, 1.0f);
}
#endif