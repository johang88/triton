using BulletSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Physics.Resources
{
	public class Mesh : IDisposable
	{
		internal CollisionShape Shape;

        public void Dispose()
        {
            Shape?.Dispose();
            Shape = null;
        }

		internal void Build(bool isConvexHull, List<BulletSharp.Math.Vector3> vertices, List<int> indices)
		{
			if (isConvexHull)
			{
				Shape = new ConvexHullShape(vertices);
			}
			else
			{
                var mesh = new TriangleMesh();
                
                for (var i = 0; i < indices.Count; i += 3)
                {
                    mesh.AddTriangle(
                        vertices[indices[i + 0]],
                        vertices[indices[i + 1]],
                        vertices[indices[i + 2]]
                        );
                }

                Shape = new BvhTriangleMeshShape(mesh, false);
            }
		}
	}
}
