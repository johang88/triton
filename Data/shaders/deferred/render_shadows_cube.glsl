#ifdef VERTEX_SHADER

attrib(vec3, iPosition, Position);

out vec4 position;

uniform(mat4x4, modelViewProjection, ModelViewProjection);

void main()
{
	position = modelViewProjection * vec4(iPosition, 1);
	gl_Position = modelViewProjection * vec4(iPosition, 1);
}

#elif defined(GEOMETRY_SHADER)

void main()
{
}

#else

in vec4 position;

out(float, fragmentdepth, 0);
uniform(vec2, clipPlane, ClipPlane);

void main()
{
	float depth = position.z / (clipPlane.y - clipPlane.x);
	fragmentdepth = depth;
	gl_FragDepth = depth;
}
#endif