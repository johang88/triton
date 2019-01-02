using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Renderer;
using Triton.Renderer.RenderTargets;
using Triton.Utility;

namespace Triton.Graphics.Post.Effects
{
	public class SMAA : BaseEffect
	{
		private Resources.ShaderProgram[] _edgeShader = new Resources.ShaderProgram[4];
		private Resources.ShaderProgram[] _blendShader = new Resources.ShaderProgram[4];
		private Resources.ShaderProgram[] _neighborhoodShader = new Resources.ShaderProgram[4];

		private readonly EdgeShaderParams[] _edgeParams = new EdgeShaderParams[4];
		private readonly BlendShaderParams[] _blendParams = new BlendShaderParams[4];
		private readonly NeighborhoodShaderParams[] _neighborhoodParams = new NeighborhoodShaderParams[4];

		private readonly RenderTarget _edgeRenderTarget;
		private readonly RenderTarget _blendRenderTarget;

		private Resources.Texture _areaTexture;
		private Resources.Texture _searchTexture;

		private readonly int _linearClampSampler = 0;
		private readonly int _nearClampSampler = 0;

        public int EdgeRenderTargetTexture => _edgeRenderTarget.Textures[0].Handle;
        public int BlendRenderTarget => _blendRenderTarget.Textures[0].Handle;

        public SMAA(Backend backend, IO.FileSystem fileSystem, BatchBuffer quadMesh)
			: base(backend, quadMesh)
		{
			var width = _backend.Width;
			var height = _backend.Height;

			_areaTexture = _backend.CreateTexture("/textures/smaa_area", 160, 560, PixelFormat.Rg, PixelInternalFormat.Rg8, PixelType.UnsignedByte, GetRawTexture(fileSystem, "/textures/smaa_area.raw"), false);
			_searchTexture = _backend.CreateTexture("/textures/smaa_search", 66, 33, PixelFormat.Red, PixelInternalFormat.R8, PixelType.UnsignedByte, GetRawTexture(fileSystem, "/textures/smaa_search.raw"), false);

			_linearClampSampler = _backend.RenderSystem.CreateSampler(new Dictionary<SamplerParameterName, int>
			{
				{ SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.Linear },
				{ SamplerParameterName.TextureMagFilter, (int)TextureMagFilter.Linear },
				{ SamplerParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge },
				{ SamplerParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge }
			});

			_nearClampSampler = _backend.RenderSystem.CreateSampler(new Dictionary<SamplerParameterName, int>
			{
				{ SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest },
				{ SamplerParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest },
				{ SamplerParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge },
				{ SamplerParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge }
			});

			_edgeRenderTarget = _backend.CreateRenderTarget("smaa_edge", new Definition(width, height, false, new List<Definition.Attachment>()
			{
				new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 0),
			}));

			_blendRenderTarget = _backend.CreateRenderTarget("smaa_blend", new Definition(width, height, false, new List<Definition.Attachment>()
			{
				new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 0),
			}));
		}

		internal override void LoadResources(Triton.Resources.ResourceManager resourceManager)
		{
			base.LoadResources(resourceManager);

			var width = _backend.Width;
			var height = _backend.Height;

			var pixelSizeDefine = string.Format("SMAA_PIXEL_SIZE float2(1.0 / {0}, 1.0 / {1})", StringConverter.ToString(width), StringConverter.ToString(height));
			var qualities = new string[] { "SMAA_PRESET_LOW", "SMAA_PRESET_MEDIUM", "SMAA_PRESET_HIGH", "SMAA_PRESET_ULTRA" };

			for (var i = 0; i < 4; i++)
			{
				var defines = pixelSizeDefine + ";" + qualities[i] + " 1";

				_edgeShader[i] = resourceManager.Load<Resources.ShaderProgram>("/shaders/post/smaa_edge", defines);
				_blendShader[i] = resourceManager.Load<Resources.ShaderProgram>("/shaders/post/smaa_blend", defines);
				_neighborhoodShader[i] = resourceManager.Load<Resources.ShaderProgram>("/shaders/post/smaa_neighborhood", defines);
			}
		}

		private byte[] GetRawTexture(IO.FileSystem fileSystem, string path)
		{
			using (var stream = fileSystem.OpenRead(path))
			{
				var buffer = new byte[stream.Length];
				var bytesRead = 0;

				while (bytesRead < (int)stream.Length)
				{
					bytesRead += stream.Read(buffer, bytesRead, (int)(stream.Length - bytesRead));
				}

				return buffer;
			}
		}

		public void Render(AntiAliasingQuality quality, RenderTarget input, RenderTarget output)
		{
			if (_edgeParams[0] == null)
			{
				for (var i = 0; i < 4; i++)
				{
					_edgeParams[i] = new EdgeShaderParams();
					_blendParams[i] = new BlendShaderParams();
					_neighborhoodParams[i] = new NeighborhoodShaderParams();

					_edgeShader[i].BindUniformLocations(_edgeParams[i]);
					_blendShader[i].BindUniformLocations(_blendParams[i]);
					_neighborhoodShader[i].BindUniformLocations(_neighborhoodParams[i]);
				}
			}

			var index = (int)quality;

			// Edge
			_backend.BeginPass(_edgeRenderTarget, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
			_backend.BeginInstance(_edgeShader[index].Handle, new int[] { input.Textures[0].Handle },
				samplers: new int[] { _linearClampSampler });
			_backend.BindShaderVariable(_edgeParams[index].SamplerScene, 0);

			_backend.DrawMesh(_quadMesh.MeshHandle);
			_backend.EndPass();

			// Blend
			_backend.BeginPass(_blendRenderTarget, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
			_backend.BeginInstance(_blendShader[index].Handle, new int[] { _edgeRenderTarget.Textures[0].Handle, _areaTexture.Handle, _searchTexture.Handle },
				samplers: new int[] { _linearClampSampler, _linearClampSampler, _nearClampSampler });
			_backend.BindShaderVariable(_blendParams[index].SamplerEdge, 0);
			_backend.BindShaderVariable(_blendParams[index].SamplerArea, 1);
			_backend.BindShaderVariable(_blendParams[index].SamplerSearch, 2);

			_backend.DrawMesh(_quadMesh.MeshHandle);
			_backend.EndPass();

			// Neighborhood
			_backend.BeginPass(output, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
			_backend.BeginInstance(_neighborhoodShader[index].Handle, new int[] { input.Textures[0].Handle, _blendRenderTarget.Textures[0].Handle },
				samplers: new int[] { _linearClampSampler, _linearClampSampler });
			_backend.BindShaderVariable(_neighborhoodParams[index].SamplerScene, 0);
			_backend.BindShaderVariable(_neighborhoodParams[index].SamplerBlend, 1);

			_backend.DrawMesh(_quadMesh.MeshHandle);
			_backend.EndPass();
		}

		public override void Resize(int width, int height)
		{
			base.Resize(width, height);

			_backend.ResizeRenderTarget(_edgeRenderTarget, width, height);
			_backend.ResizeRenderTarget(_blendRenderTarget, width, height);
		}

		class EdgeShaderParams
		{
			public int SamplerScene = 0;
		}

		class BlendShaderParams
		{
			public int SamplerEdge = 0;
			public int SamplerArea = 0;
			public int SamplerSearch = 0;
		}

		class NeighborhoodShaderParams
		{
			public int SamplerScene = 0;
			public int SamplerBlend = 0;
		}
	}
}
