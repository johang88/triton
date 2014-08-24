using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Terrain
{
	public class TerrainData
	{
		private readonly ushort[] Data;
		private readonly int Size;
		public readonly Vector3 WorldSize;

		public TerrainData(System.IO.Stream stream, Vector3 worldSize, int size)
		{
			Size = size;
			WorldSize = worldSize;

			Data = new ushort[size * size];
			using (var reader = new System.IO.BinaryReader(stream))
			{
				var i = 0;
				while (reader.BaseStream.Position < reader.BaseStream.Length)
					Data[i++] = reader.ReadUInt16();
			}
		}

		public float GetHeightAt(float x, float z)
		{
			x /= WorldSize.X;
			z /= WorldSize.Z;

			x *= Size;
			z *= Size;

			if (x < 0.0f || x >= Size || z < 0 || z >= Size)
				return -1;

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
			height *= WorldSize.Y;

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

			if (down.Z >=  WorldSize.Z - 1)
			{
				flip *= -1;
				down = new Vector3(x, GetHeightAt(x, z - 1.0f), z - 1.0f);
			}

			left -= here;
			down -= here;

			var normal = Vector3.Cross(left, down) * flip;
			normal = normal.Normalize();

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
			return Data[z * Size + x] / (float)ushort.MaxValue;
		}
	}
}
