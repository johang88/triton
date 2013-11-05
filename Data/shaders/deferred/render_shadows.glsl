#ifdef VERTEX_SHADER

attrib(vec3, iPosition, Position);

out vec4 position;

uniform(mat4x4, modelViewProjection, ModelViewProjection);

void main()
{
	position = modelViewProjection * vec4(iPosition, 1);
	gl_Position = modelViewProjection * vec4(iPosition, 1);
}

#else

in vec4 position;

out(vec4, oColor, 0);

void main()
{
	float depth = position.z / 100.0f;
	
	/*float dx = dFdx(position.z);
	float dy = dFdy(position.w);
	
	depth += 0.5f * (dx * dx + dy * dy);*/
	
	oColor = vec4(depth.xxx, 1.0f);
}
#endif