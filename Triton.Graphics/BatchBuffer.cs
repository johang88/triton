using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics
{
	/// <summary>
	/// Efficent implementation of a dynamic vertex buffer
	/// </summary>
	public class BatchBuffer : IDisposable
	{
		private static int BatchBufferCount = 0;

		private bool Disposed = false;
		private float[] VertexData;
		private int[] IndexData;
		private int TriangleCount;
		private int DataCount;
		private Renderer.RenderSystem RenderSystem;

		public Resources.Mesh Mesh { get; private set; }

		internal BatchBuffer(Renderer.RenderSystem renderSystem, int initialTriangleCount = 128)
		{
			if (renderSystem == null)
				throw new ArgumentNullException("renderSystem");
			if (initialTriangleCount <= 0)
				throw new ArgumentException("invalid initialTriangleCount");

			RenderSystem = renderSystem;

			var dataCount = initialTriangleCount * (3 + 3 + 3 + 2); // vec3 pos, vec3 normal, vec3 tangent, vec2 texcoord
			VertexData = new float[dataCount];
			IndexData = new int[initialTriangleCount];

			Mesh = new Resources.Mesh("__sys/batch_buffer_/" + Common.StringConverter.ToString(++BatchBufferCount) + ".mesh", "");
			Mesh.Handles = new int[] { renderSystem.CreateMesh(0, null, null, true, null) };
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool isDisposing)
		{
			if (!isDisposing || Disposed)
				return;

			RenderSystem.DestroyMesh(Mesh.Handles[0]);
			Disposed = true;
		}

		private void CheckSize(int requiredComponents)
		{
			var requiredSize = DataCount + requiredComponents;
			if (VertexData.Length <= requiredSize)
			{
				Array.Resize(ref VertexData, VertexData.Length * 2);
			}
		}

		public void Begin()
		{
			TriangleCount = 0;
			DataCount = 0;
		}

		public void End()
		{
			RenderSystem.SetMeshData(Mesh.Handles[0], TriangleCount, VertexData, IndexData, true, null);
		}

		public void AddVector2(float x, float y)
		{
			CheckSize(2);

			VertexData[DataCount++] = x;
			VertexData[DataCount++] = y;
		}

		public void AddVector2(ref Vector2 v)
		{
			CheckSize(3);

			VertexData[DataCount++] = v.X;
			VertexData[DataCount++] = v.Y;
		}

		public void AddVector3(float x, float y, float z)
		{
			CheckSize(3);

			VertexData[DataCount++] = x;
			VertexData[DataCount++] = y;
			VertexData[DataCount++] = z;
		}

		public void AddVector3(ref Vector3 v)
		{
			CheckSize(3);

			VertexData[DataCount++] = v.X;
			VertexData[DataCount++] = v.Y;
			VertexData[DataCount++] = v.Z;
		}

		public void AddVector4(float x, float y, float z, float w)
		{
			CheckSize(4);

			VertexData[DataCount++] = x;
			VertexData[DataCount++] = y;
			VertexData[DataCount++] = z;
			VertexData[DataCount++] = w;
		}

		public void AddVector4(ref Vector4 v)
		{
			CheckSize(4);

			VertexData[DataCount++] = v.X;
			VertexData[DataCount++] = v.Y;
			VertexData[DataCount++] = v.Z;
			VertexData[DataCount++] = v.W;
		}

		public void AddTriangle(int v1, int v2, int v3)
		{
			var requiredIndexBufferSize = TriangleCount + 3;
			if (IndexData.Length <= requiredIndexBufferSize)
			{
				Array.Resize(ref IndexData, IndexData.Length * 2);
			}

			IndexData[TriangleCount++] = v1;
			IndexData[TriangleCount++] = v2;
			IndexData[TriangleCount++] = v3;
		}

		public void AddQuad(Vector2 position, Vector2 size, Vector2 uvPositon, Vector2 uvSize)
		{
			var firstIndex = DataCount / (3 + 3 + 3 + 2);

			AddVector3(position.X, position.Y, 0);
			AddVector3(0, 0, 0); // normal
			AddVector3(0, 0, 0); // tangent
			AddVector2(ref uvPositon);

			AddVector3(position.X, position.Y + size.Y, 0);
			AddVector3(0, 0, 0); // normal
			AddVector3(0, 0, 0); // tangent
			AddVector2(uvPositon.X, uvPositon.Y + uvSize.Y);

			AddVector3(position.X + size.X, position.Y + size.Y, 0);
			AddVector3(0, 0, 0); // normal
			AddVector3(0, 0, 0); // tangent
			AddVector2(uvPositon.X + uvSize.X, uvPositon.Y + uvSize.Y);

			AddVector3(position.X + size.X, position.Y, 0);
			AddVector3(0, 0, 0); // normal
			AddVector3(0, 0, 0); // tangent
			AddVector2(uvPositon.X + uvSize.X, uvPositon.Y);

			AddTriangle(firstIndex, firstIndex + 1, firstIndex + 2);
			AddTriangle(firstIndex, firstIndex + 2, firstIndex + 3);
		}
	}
}
