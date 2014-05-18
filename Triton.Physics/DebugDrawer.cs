using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Graphics;
using Triton.Renderer;
using Triton.Common;

namespace Triton.Physics
{
	class DebugDrawer : Jitter.IDebugDrawer
	{
		private readonly Backend Backend;
		private readonly BatchBuffer Batch;
		private readonly Graphics.Resources.ShaderProgram Shader;
		private ShaderParams Params = null;
		public Vector3 Color = new Vector3(0, 1, 0);
		private int TriangleIndex = 0;
		private int RenderStateId;

		public DebugDrawer(Backend backend, ResourceManager resourceManager)
		{
			if (backend == null)
				throw new ArgumentNullException("backend");

			Backend = backend;
			Batch = Backend.CreateBatchBuffer(new Renderer.VertexFormat(new Renderer.VertexFormatElement[]
				{
					new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.Position, Renderer.VertexPointerType.Float, 3, 0),
					new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.Color, Renderer.VertexPointerType.Float, 3, sizeof(float) * 3),
				}));
			Batch.Begin();

			Shader = resourceManager.Load<Graphics.Resources.ShaderProgram>("shaders/physic_debug_drawer");
			RenderStateId = backend.CreateRenderState(false, true, true, BlendingFactorSrc.One, BlendingFactorDest.One);
		}

		public void DrawLine(Jitter.LinearMath.JVector start, Jitter.LinearMath.JVector end)
		{
			// TODO
		}

		public void DrawPoint(Jitter.LinearMath.JVector pos)
		{
			// TODO
		}

		public void DrawTriangle(Jitter.LinearMath.JVector pos1, Jitter.LinearMath.JVector pos2, Jitter.LinearMath.JVector pos3)
		{
			Batch.AddVector3(pos1.X, pos1.Y, pos1.Z);
			Batch.AddVector3(ref Color);

			Batch.AddVector3(pos2.X, pos2.Y, pos2.Z);
			Batch.AddVector3(ref Color);

			Batch.AddVector3(pos3.X, pos3.Y, pos3.Z);
			Batch.AddVector3(ref Color);

			Batch.AddTriangle(TriangleIndex + 0, TriangleIndex + 2, TriangleIndex + 1);
			TriangleIndex += 3;
		}

		public void Render(Camera camera)
		{
			if (Params == null)
			{
				Params = new ShaderParams();
				Shader.GetUniformLocations(Params);
			}

			Matrix4 viewMatrix;
			camera.GetViewMatrix(out viewMatrix);

			Matrix4 projectionMatrix;
			camera.GetProjectionMatrix(out projectionMatrix);

			var modelViewProjectionMatrix = viewMatrix * projectionMatrix;

			Batch.End();

			Backend.BeginInstance(Shader.Handle, new int[0], null, RenderStateId);

			Backend.BindShaderVariable(Params.HandleModelViewProjection, ref modelViewProjectionMatrix);
			Backend.BindShaderVariable(Params.HandleColor, ref Color);

			Backend.DrawMesh(Batch.MeshHandle);
			Backend.EndInstance();

			TriangleIndex = 0;
			Batch.Begin();
		}

		class ShaderParams
		{
			public int HandleModelViewProjection = 0;
			public int HandleColor = 0;
		}
	}
}
