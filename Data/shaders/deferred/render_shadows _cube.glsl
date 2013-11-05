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
	for (int layer = 0; layer < 6; layer++) {
		gl_Layer = layer;
		for (i = 0; i < 3; i++) {
			gl_Position = gl_PositionIn[i];
			EmitVertex();
		}
		EndPrimitive();
	}
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