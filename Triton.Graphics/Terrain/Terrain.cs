using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Renderer;

namespace Triton.Graphics.Terrain
{
	public class Terrain
	{
		private readonly Backend Backend;
		private readonly VertexFormat VertexFormat;

		private readonly int Size;
		private readonly int Res;
		private readonly float MaxHeight;
		private readonly float Width;
		private readonly float Depth;

		private readonly int Handle;
		public readonly Resources.Mesh Mesh;

		private short[] Data;

		public Vector3 Position = Vector3.Zero;

		public Terrain(System.IO.Stream stream, Backend backend, float width, float depth, float maxHeight, int res, int size, int uvTile, Resources.Material material)
		{
			if (backend == null)
				throw new ArgumentNullException("backend");

			Backend = backend;

			VertexFormat = new VertexFormat(new VertexFormatElement[] {
				new VertexFormatElement(VertexFormatSemantic.Position, VertexPointerType.Float, 3, sizeof(float) * 0),
				new VertexFormatElement(VertexFormatSemantic.Normal, VertexPointerType.Float, 3, sizeof(float) * 3),
				new VertexFormatElement(VertexFormatSemantic.Tangent, VertexPointerType.Float, 3, sizeof(float) * 6),
				new VertexFormatElement(VertexFormatSemantic.TexCoord, VertexPointerType.Float, 2, sizeof(float) * 9)
			});

			Size = size;
			Res = res;
			MaxHeight = maxHeight;
			Width = width;
			Depth = depth;

			Data = new short[size * size];
			using (var reader = new System.IO.BinaryReader(stream))
			{
				var i = 0;
				while (reader.BaseStream.Position < reader.BaseStream.Length)
					Data[i++] = reader.ReadInt16();
			}

			var vres = Res + 1;

			var vertices = new Vertex[vres * vres];
			var indices = new int[6 * Res * Res];
			var iw = 6 * Res;

			var w = Width / (float)Res;
			var d = Depth / (float)Res;

			// Vertices
			for (var z = 0; z < vres; z++)
			{
				for (var x = 0; x < vres; x++)
				{
					var _x = (float)x * w;
					var _z = (float)z * d;
					var _y = GetHeightAt(_x, _z);

					var p = new Vector3(_x, _y, _z);

					vertices[x * vres + z] = new Vertex(p);
					vertices[x * vres + z].TexCoord = new Vector2(x / ((float)Res / uvTile), z / ((float)Res / uvTile));
					vertices[x * vres + z].Normal = GetNormalAt(_x, _z);
					vertices[x * vres + z].Tangent = GetTangentAt(_x, _z);
				}
			}

			// Indices
			for (var x = 0; x < Res; x++)
			{
				for (var z = 0; z < Res; z++)
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

			Handle = Backend.CreateMesh(indices.Length / 3, VertexFormat, vertices, indices, false);
			Mesh = new Resources.Mesh("_sys/terrain", "");
			Mesh.SubMeshes = new Resources.SubMesh[] { new Resources.SubMesh { Handle = Handle, Material = material } };
		}

		public float GetHeightAt(float x, float z)
		{
			x -= Position.X;
			z -= Position.Z;

			x /= Width;
			z /= Depth;

			x *= Size;
			z *= Size;

			if (x < 0.0f || x >= Size || z < 0 || z >= Size)
				return float.NegativeInfinity;

			int xi = (int)x, zi = (int)z;
			float xpct = x - xi, zpct = z - zi;

			if (xi == Size - 1)
			{
				--xi;
				xpct = 1.0f;
			}
			if (zi == Size - 1)
			{
				--zi;
				zpct = 1.0f;
			}

			var heights = new float[]
			{
				At(xi, zi),
				At(xi, zi + 1),
				At(xi + 1, zi),
				At(xi + 1, zi + 1)
			};

			var w = new float[]
			{
				(1.0f - xpct) * (1.0f - zpct),
				(1.0f - xpct) * zpct,
				xpct * (1.0f - zpct),
				xpct * zpct
			};

			var height = w[0] * heights[0] + w[1] * heights[1] + w[2] * heights[2] + w[3] * heights[3];
			height *= MaxHeight;
			height += Position.Y;

			return height;
		}

		public Vector3 GetNormalAt(float x, float z)
		{
			var flip = 1;
			var here = new Vector3(x, GetHeightAt(x, z), z);
			var left = new Vector3(x - 1.0f, GetHeightAt(x - 1.0f, z), z);
			var down = new Vector3(x, GetHeightAt(x, z + 1.0f), z + 1.0f);

			if (left.X < 0.0f)
			{
				flip *= -1;
				left = new Vector3(x + 1.0f, GetHeightAt(x + 1.0f, z), z);
			}

			if (down.Z >= Position.Z + Depth - 1)
			{
				flip *= -1;
				down = new Vector3(x, GetHeightAt(x, z - 1.0f), z - 1.0f);
			}

			left -= here;
			down -= here;

			var normal = Vector3.Cross(left, down) * flip;
			normal.Normalize();

			return normal;
		}

		public Vector3 GetTangentAt(float x, float z)
		{
			var flip = 1;
			var here = new Vector3(x, GetHeightAt(x, z), z);
			var left = new Vector3(x - 1, GetHeightAt(x - 1, z), z);
			if (left.X < 0.0f)
			{
				flip *= -1;
				left = new Vector3(x + 1, GetHeightAt(x + 1, z), z);
			}

			left -= here;

			var tangent = left * flip;
			tangent.Normalize();

			return tangent;
		}

		private float At(int x, int z)
		{
			return Data[z * Size + x] / (float)short.MaxValue;
		}

		struct Vertex
		{
			public Vertex(Vector3 position)
			{
				Position = position;
				Normal = new Vector3(0, 0, 0);
				TexCoord = Vector2.Zero;
				Tangent = Vector3.Zero;
			}

			public Vector3 Position;
			public Vector3 Normal;
			public Vector3 Tangent;
			public Vector2 TexCoord;
		}
	}
}
