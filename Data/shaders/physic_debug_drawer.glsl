#ifdef VERTEX_SHADER

attrib(vec3, iPosition, Position);

attrib(vec3, iColor, Color);
out vec3 color;

uniform(mat4x4, modelViewProjection, ModelViewProjection);

void main()
{
	color = iColor;
	gl_Position = modelViewProjection * vec4(iPosition, 1);
}

#else

in vec3 color;

out(vec4, oColor, 0);

void main()
{
	oColor = vec4(color, 1.0f);
}
#endif