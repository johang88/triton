using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Renderer;

namespace Triton.Graphics.Terrain
{
	class Chunk
	{
		private readonly Terrain Terrain;
		private readonly Vector3 Center;
		internal readonly Resources.SubMesh Mesh;
		internal Resources.Material SplatMaterial;
		internal Resources.Material CompositeMaterial;

		public Chunk(Terrain terrain, Vector2 chunkPosition, Vector2 chunkSize, int resolution, int chunks)
		{
			Terrain = terrain;
			Mesh = new Resources.SubMesh();

			var vertexFormat = new VertexFormat(new VertexFormatElement[] {
				new VertexFormatElement(VertexFormatSemantic.Position, VertexPointerType.Float, 3, sizeof(float) * 0),
				new VertexFormatElement(VertexFormatSemantic.Normal, VertexPointerType.Float, 3, sizeof(float) * 3),
				new VertexFormatElement(VertexFormatSemantic.Tangent, VertexPointerType.Float, 3, sizeof(float) * 6),
				new VertexFormatElement(VertexFormatSemantic.TexCoord, VertexPointerType.Float, 2, sizeof(float) * 9),
				new VertexFormatElement(VertexFormatSemantic.TexCoord2, VertexPointerType.Float, 2, sizeof(float) * 11)
			});

			Center = new Vector3(chunkPosition.X + chunkSize.X / 2.0f, 0, chunkPosition.Y + chunkPosition.Y / 2.0f);

			// Create mesh
			var vres = resolution + 1;

			var vertices = new Vertex[vres * vres];
			var indices = new int[6 * resolution * resolution];
			var iw = 6 * resolution;

			var w = chunkSize.X / (float)resolution;
			var d = chunkSize.Y / (float)resolution;

			// Vertices
			for (var z = 0; z < vres; z++)
			{
				for (var x = 0; x < vres; x++)
				{
					var _x = chunkPosition.X + (float)x * w;
					var _z = chunkPosition.Y + (float)z * d;
					var _y = Terrain.Data.GetHeightAt(_x, _z);

					var p = new Vector3(_x, _y, _z);

					vertices[x * vres + z] = new Vertex(p);
					vertices[x * vres + z].TexCoord = new Vector2(p.X / Terrain.Data.WorldSize.X, p.Z / Terrain.Data.WorldSize.Z);
					vertices[x * vres + z].TexCoord2 = new Vector2(p.X / Terrain.Data.WorldSize.X, p.Z / Terrain.Data.WorldSize.Z) * Terrain.Data.WorldSize.X;
					vertices[x * vres + z].Normal = Terrain.Data.GetNormalAt(_x, _z);
					vertices[x * vres + z].Tangent = Terrain.Data.GetTangentAt(_x, _z);
				}
			}

			// Indices
			for (var x = 0; x < resolution; x++)
			{
				for (var z = 0; z < resolution; z++)
				{
					var zi = z * 6;

					indices[x * iw + zi + 0] = (int)(x * vres + z + 0);
					indices[x * iw + zi + 1] = (int)(x * vres + z + 1);
					indices[x * iw + zi + 2] = (int)(x * vres + z + vres);

					indices[x * iw + zi + 3] = (int)(x * vres + z + vres + 1);
					indices[x * iw + zi + 4] = (int)(x * vres + z + vres);
					indices[x * iw + zi + 5] = (int)(x * vres + z + 1);
				}
			}

			// Upload mesh data
			Mesh.VertexBufferHandle = Terrain.Backend.RenderSystem.CreateBuffer(Renderer.BufferTarget.ArrayBuffer, false, vertexFormat);
			Mesh.IndexBufferHandle = Terrain.Backend.RenderSystem.CreateBuffer(Renderer.BufferTarget.ElementArrayBuffer, false);

			Terrain.Backend.RenderSystem.SetBufferData(Mesh.VertexBufferHandle, vertices, false, true);
			Terrain.Backend.RenderSystem.SetBufferData(Mesh.IndexBufferHandle, indices, false, true);

			Mesh.Handle = Terrain.Backend.RenderSystem.CreateMesh(indices.Length / 3, Mesh.VertexBufferHandle, Mesh.IndexBufferHandle, true);
			Mesh.BoundingSphereRadius = 10000;
		}

		public void Update(Vector3 cameraPosition)
		{
			// TODO: Select material and things
			cameraPosition.Y = 0;
			var distance = (cameraPosition - Center).Length;

			// TODO: maybe not hardcode ...
			Mesh.Material = distance > (4096 / 5.0f) ? CompositeMaterial : SplatMaterial;
		}

		struct Vertex
		{
			public Vertex(Vector3 position)
			{
				Position = position;
				Normal = new Vector3(0, 0, 0);
				TexCoord = Vector2.Zero;
				Tangent = Vector3.Zero;
				TexCoord2 = Vector2.Zero;
			}

			public Vector3 Position;
			public Vector3 Normal;
			public Vector3 Tangent;
			public Vector2 TexCoord;
			public Vector2 TexCoord2;
		}
	}
}
