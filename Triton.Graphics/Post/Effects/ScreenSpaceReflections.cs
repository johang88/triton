using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Post.Effects
{
	public class ScreenSpaceReflections : BaseEffect
	{
		private Resources.ShaderProgram Shader;
		private ScreenSpaceReflectionsShaderParams ShaderParams;

		public ScreenSpaceReflections(Backend backend, Common.ResourceManager resourceManager, BatchBuffer quadMesh)
			: base(backend, resourceManager, quadMesh)
		{
			Shader = ResourceManager.Load<Resources.ShaderProgram>("/shaders/post/screenspacereflections");
		}

		public void Render(Camera camera, RenderTarget gbuffer, RenderTarget input, RenderTarget output)
		{
			if (ShaderParams == null)
			{
				ShaderParams = new ScreenSpaceReflectionsShaderParams();
				Shader.GetUniformLocations(ShaderParams);
			}

			Matrix4 viewMatrix;
			camera.GetViewMatrix(out viewMatrix);

			Matrix4 projectionMatrix;
			camera.GetProjectionMatrix(out projectionMatrix);

			var viewProjectionMatrix = viewMatrix * projectionMatrix;

			var cameraClipPlane = new Vector2(camera.NearClipDistance, camera.FarClipDistance);

			var itView = Matrix4.Transpose(Matrix4.Invert(viewMatrix));

			Backend.BeginPass(output, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
			Backend.BeginInstance(Shader.Handle, new int[] { input.Textures[0].Handle, gbuffer.Textures[1].Handle, gbuffer.Textures[3].Handle },
				samplers: new int[] { Backend.DefaultSamplerNoFiltering, Backend.DefaultSamplerNoFiltering, Backend.DefaultSamplerNoFiltering });
			Backend.BindShaderVariable(ShaderParams.SamplerScene, 0);
			Backend.BindShaderVariable(ShaderParams.SamplerNormal, 1);
			Backend.BindShaderVariable(ShaderParams.SamplerDepth, 2);
			Backend.BindShaderVariable(ShaderParams.CameraPosition, ref camera.Position);
			Backend.BindShaderVariable(ShaderParams.ViewProjectionMatrix, ref projectionMatrix);
			Backend.BindShaderVariable(ShaderParams.CameraClipPlane, ref cameraClipPlane);
			Backend.BindShaderVariable(ShaderParams.ItView, ref itView);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();
		}

		class ScreenSpaceReflectionsShaderParams
		{
			public int SamplerScene = 0;
			public int SamplerNormal = 0;
			public int SamplerDepth = 0;
			public int CameraPosition = 0;
			public int ViewProjectionMatrix = 0;
			public int CameraClipPlane = 0;
			public int ItView = 0;
		}
	}
}
