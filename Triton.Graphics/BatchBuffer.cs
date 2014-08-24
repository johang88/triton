using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics
{
	/// <summary>
	/// Efficent implementation of a dynamic vertex buffer. 
	/// Allows you to add any number of vertices. 
	/// 
	/// Indexed rendering is mandatory. Use AddTriangle to add indicies to the index buffer.
	/// 
	/// The vertex format is fixed currently so the usability of this class is limited.
	/// </summary>
	public class BatchBuffer : IDisposable
	{
		private bool Disposed = false;
		private float[] VertexData;
		private int[] IndexData;
		private int IndexCount;
		private int DataCount;
		private Renderer.RenderSystem RenderSystem;

		public readonly int MeshHandle;
		private readonly int VertexBufferHandle;
		private readonly int IndexBufferHandle;

		private readonly Renderer.VertexFormat VertexFormat;

		internal BatchBuffer(Renderer.RenderSystem renderSystem, Renderer.VertexFormat vertexFormat = null, int initialTriangleCount = 128)
		{
			if (renderSystem == null)
				throw new ArgumentNullException("renderSystem");
			if (initialTriangleCount <= 0)
				throw new ArgumentException("invalid initialTriangleCount");

			RenderSystem = renderSystem;

			var dataCount = initialTriangleCount * (3 + 3 + 3 + 2); // vec3 pos, vec3 normal, vec3 tangent, vec2 texcoord
			VertexData = new float[dataCount];
			IndexData = new int[initialTriangleCount];

			if (vertexFormat != null)
			{
				VertexFormat = vertexFormat;
			}
			else
			{
				VertexFormat = new Renderer.VertexFormat(new Renderer.VertexFormatElement[]
				{
					new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.Position, Renderer.VertexPointerType.Float, 3, 0),
					new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.TexCoord, Renderer.VertexPointerType.Float, 2, sizeof(float) * 3),
				});
			}

			VertexBufferHandle = RenderSystem.CreateBuffer(Renderer.BufferTarget.ArrayBuffer, VertexFormat);
			IndexBufferHandle = RenderSystem.CreateBuffer(Renderer.BufferTarget.ElementArrayBuffer);

			RenderSystem.SetBufferData(VertexBufferHandle, new byte[0], true);
			RenderSystem.SetBufferData(IndexBufferHandle, new byte[0], true);
			
			MeshHandle = RenderSystem.CreateMesh(0, VertexBufferHandle, IndexBufferHandle, null);
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

			RenderSystem.DestroyMesh(MeshHandle);
			RenderSystem.DestroyBuffer(VertexBufferHandle);
			RenderSystem.DestroyBuffer(IndexBufferHandle);

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
			IndexCount = 0;
			DataCount = 0;
		}

		public void End()
		{
			RenderSystem.MeshSetTriangleCount(MeshHandle, IndexCount / 3, true);
			RenderSystem.SetBufferData(VertexBufferHandle, VertexData, true);
			RenderSystem.SetBufferData(IndexBufferHandle, IndexData, true);
		}

		public void EndInline(Backend backend)
		{
			backend.UpdateMeshInline(MeshHandle, IndexCount / 3, DataCount, IndexCount, VertexData, IndexData, true);
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
			var requiredIndexBufferSize = IndexCount + 3;
			if (IndexData.Length <= requiredIndexBufferSize)
			{
				Array.Resize(ref IndexData, IndexData.Length * 2);
			}

			IndexData[IndexCount++] = v1;
			IndexData[IndexCount++] = v2;
			IndexData[IndexCount++] = v3;
		}

		/// <summary>
		/// Utility method to add a single 2d quad (2 triangles) to the buffer.
		/// Indexes will be setup automatically so there is no need to call AddTriangle manually.
		/// </summary>
		/// <param name="position">Position of the quad</param>
		/// <param name="size">Size of the quad, relative to the position</param>
		/// <param name="uvPositon">UV Position of the quad</param>
		/// <param name="uvSize">UV Size of the quad, relative to the UV position</param>
		public void AddQuad(Vector2 position, Vector2 size, Vector2 uvPositon, Vector2 uvSize)
		{
			var firstIndex = DataCount / (VertexFormat.Size / sizeof(float));

			AddVector3(position.X, position.Y, 0);
			AddVector2(ref uvPositon);

			AddVector3(position.X, position.Y + size.Y, 0);
			AddVector2(uvPositon.X, uvPositon.Y + uvSize.Y);

			AddVector3(position.X + size.X, position.Y + size.Y, 0);
			AddVector2(uvPositon.X + uvSize.X, uvPositon.Y + uvSize.Y);

			AddVector3(position.X + size.X, position.Y, 0);
			AddVector2(uvPositon.X + uvSize.X, uvPositon.Y);

			AddTriangle(firstIndex, firstIndex + 2, firstIndex + 1);
			AddTriangle(firstIndex, firstIndex + 3, firstIndex + 2);
		}

		public void AddQuadInverseUV(Vector2 position, Vector2 size, Vector2 uvPositon, Vector2 uvSize, Vector4 color)
		{
			var firstIndex = DataCount / (VertexFormat.Size / sizeof(float));

			AddVector3(position.X, position.Y, 0);
			AddVector2(uvPositon.X, uvPositon.Y + uvSize.Y);
			AddVector4(ref color);

			AddVector3(position.X, position.Y + size.Y, 0);
			AddVector2(ref uvPositon);
			AddVector4(ref color);

			AddVector3(position.X + size.X, position.Y + size.Y, 0);
			AddVector2(uvPositon.X + uvSize.X, uvPositon.Y);
			AddVector4(ref color);

			AddVector3(position.X + size.X, position.Y, 0);
			AddVector2(uvPositon.X + uvSize.X, uvPositon.Y + uvSize.Y);
			AddVector4(ref color);

			AddTriangle(firstIndex, firstIndex + 2, firstIndex + 1);
			AddTriangle(firstIndex, firstIndex + 3, firstIndex + 2);
		}
	}
}
