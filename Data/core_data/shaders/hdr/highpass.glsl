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

sampler(2D, samplerScene, SceneTexture);

uniform(vec3, whitePoint, WhitePoint);
uniform(float, exposure, Exposure);

void main()
{
	vec3 scene = texture2D(samplerScene, texCoord).xyz * exposure;

	vec3 final = max(scene - whitePoint, vec3(0, 0, 0));
	
	oColor = vec4(final, 1.0f);
}
#endif