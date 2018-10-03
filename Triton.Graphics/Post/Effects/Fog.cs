using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Graphics.Resources;
using Triton.Renderer.RenderTargets;

namespace Triton.Graphics.Post.Effects
{
	public class Fog : BaseEffect
	{
		private Resources.ShaderProgram _shader;
		private ShaderParams _shaderParams;

		public Fog(Backend backend, BatchBuffer quadMesh)
			: base(backend, quadMesh)
		{
		}

		internal override void LoadResources(Common.ResourceManager resourceManager)
		{
			base.LoadResources(resourceManager);
			_shader = resourceManager.Load<Resources.ShaderProgram>("/shaders/post/fog");
		}

		public void Render(Camera camera, Stage stage, RenderTarget gbuffer, RenderTarget input, RenderTarget output)
		{
			if (_shaderParams == null)
			{
				_shaderParams = new ShaderParams();
				_shader.BindUniformLocations(_shaderParams);
			}

			Matrix4 view, projection;
			camera.GetViewMatrix(out view);
			camera.GetProjectionMatrix(out projection);

			_backend.BeginPass(output, Vector4.Zero);

			var inverseViewProjectionMatrix = Matrix4.Invert(view * projection);

			var itView = Matrix4.Invert(Matrix4.Transpose(view));
			_backend.BeginInstance(_shader.Handle,
				new int[] { gbuffer.Textures[3].Handle, input.Textures[0].Handle },
				new int[] { _backend.DefaultSamplerNoFiltering, _backend.DefaultSamplerNoFiltering });
			_backend.BindShaderVariable(_shaderParams.SamplerDepth, 0);
			_backend.BindShaderVariable(_shaderParams.SamplerScene, 1);
			_backend.BindShaderVariable(_shaderParams.InvViewProjection, ref inverseViewProjectionMatrix);
			_backend.BindShaderVariable(_shaderParams.CameraPosition, ref camera.Position);

			var sunLight = stage.GetSunLight();
            if (sunLight != null)
            {
                _backend.BindShaderVariable(_shaderParams.SunDir, ref sunLight.Direction);
            }

			_backend.DrawMesh(_quadMesh.MeshHandle);

			_backend.EndPass();
		}

		class ShaderParams
		{
			public int SamplerDepth = 0;
			public int SamplerScene = 0;
			public int InvViewProjection = 0;
			public int CameraPosition = 0;
			public int SunDir = 0;
		}
	}
}

