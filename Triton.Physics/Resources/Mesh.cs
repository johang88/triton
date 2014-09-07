﻿using Jitter.Collision;
using Jitter.Collision.Shapes;
using Jitter.LinearMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Physics.Resources
{
	public class Mesh : Triton.Common.Resource
	{
		internal Shape Shape;

		public Mesh(string name, string parameters)
			: base(name, parameters)
		{
		}

		internal void Build(bool isConvexHull, List<JVector> vertices, List<TriangleVertexIndices> indices)
		{
			if (isConvexHull)
			{
				Shape = new ConvexHullShape(vertices);
			}
			else
			{
				Shape = new TriangleMeshShape(new Octree(vertices, indices));
			}
		}
	}
}
