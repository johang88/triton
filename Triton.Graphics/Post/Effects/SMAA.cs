using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Renderer;
using Triton.Renderer.RenderTargets;

namespace Triton.Graphics.Post.Effects
{
	public class SMAA : BaseEffect
	{
		private Resources.ShaderProgram[] EdgeShader = new Resources.ShaderProgram[4];
		private Resources.ShaderProgram[] BlendShader = new Resources.ShaderProgram[4];
		private Resources.ShaderProgram[] NeighborhoodShader = new Resources.ShaderProgram[4];

		private EdgeShaderParams[] EdgeParams = new EdgeShaderParams[4];
		private BlendShaderParams[] BlendParams = new BlendShaderParams[4];
		private NeighborhoodShaderParams[] NeighborhoodParams = new NeighborhoodShaderParams[4];

		private RenderTarget EdgeRenderTarget;
		private RenderTarget BlendRenderTarget;

		private Resources.Texture AreaTexture;
		private Resources.Texture SearchTexture;

		private int LinearClampSampler = 0;
		private int NearClampSampler = 0;

		public SMAA(Backend backend, Common.IO.FileSystem fileSystem, Common.ResourceManager resourceManager, BatchBuffer quadMesh)
			: base(backend, resourceManager, quadMesh)
		{
			var width = Backend.Width;
			var height = Backend.Height;

			var pixelSizeDefine = string.Format("SMAA_PIXEL_SIZE float2(1.0 / {0}, 1.0 / {1})", Common.StringConverter.ToString(width), Common.StringConverter.ToString(height));
			var qualities = new string[] { "SMAA_PRESET_LOW", "SMAA_PRESET_MEDIUM", "SMAA_PRESET_HIGH", "SMAA_PRESET_ULTRA" };

			for (var i = 0; i < 4; i++ )
			{
				var defines = pixelSizeDefine + ";" + qualities[i] + " 1";

				EdgeShader[i] = ResourceManager.Load<Resources.ShaderProgram>("/shaders/post/smaa_edge", defines);
				BlendShader[i] = ResourceManager.Load<Resources.ShaderProgram>("/shaders/post/smaa_blend", defines);
				NeighborhoodShader[i] = ResourceManager.Load<Resources.ShaderProgram>("/shaders/post/smaa_neighborhood", defines);
			}

			LinearClampSampler = Backend.RenderSystem.CreateSampler(new Dictionary<SamplerParameterName, int>
			{
				{ SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.Linear },
				{ SamplerParameterName.TextureMagFilter, (int)TextureMagFilter.Linear },
				{ SamplerParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge },
				{ SamplerParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge }
			});

			NearClampSampler = Backend.RenderSystem.CreateSampler(new Dictionary<SamplerParameterName, int>
			{
				{ SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest },
				{ SamplerParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest },
				{ SamplerParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge },
				{ SamplerParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge }
			});

			AreaTexture = Backend.CreateTexture("/textures/smaa_area", 160, 560, PixelFormat.Rg, PixelInternalFormat.Rg8, PixelType.UnsignedByte, GetRawTexture(fileSystem, "/textures/smaa_area.raw"), false);
			SearchTexture = Backend.CreateTexture("/textures/smaa_search", 66, 33, PixelFormat.Red, PixelInternalFormat.R8, PixelType.UnsignedByte, GetRawTexture(fileSystem, "/textures/smaa_search.raw"), false);

			EdgeRenderTarget = Backend.CreateRenderTarget("smaa_edge", new Definition(width, height, false, new List<Definition.Attachment>()
			{
				new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 0),
			}));

			BlendRenderTarget = Backend.CreateRenderTarget("smaa_blend", new Definition(width, height, false, new List<Definition.Attachment>()
			{
				new Definition.Attachment(Definition.AttachmentPoint.Color, Renderer.PixelFormat.Rgba, Renderer.PixelInternalFormat.Rgba16f, Renderer.PixelType.Float, 0),
			}));
		}

		private byte[] GetRawTexture(Common.IO.FileSystem fileSystem, string path)
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
			if (EdgeParams[0] == null)
			{
				for (var i = 0; i < 4; i++)
				{
					EdgeParams[i] = new EdgeShaderParams();
					BlendParams[i] = new BlendShaderParams();
					NeighborhoodParams[i] = new NeighborhoodShaderParams();

					EdgeShader[i].GetUniformLocations(EdgeParams[i]);
					BlendShader[i].GetUniformLocations(BlendParams[i]);
					NeighborhoodShader[i].GetUniformLocations(NeighborhoodParams[i]);
				}
			}

			var index = (int)quality;

			// Edge
			Backend.BeginPass(EdgeRenderTarget, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
			Backend.BeginInstance(EdgeShader[index].Handle, new int[] { input.Textures[0].Handle },
				samplers: new int[] { LinearClampSampler });
			Backend.BindShaderVariable(EdgeParams[index].SamplerScene, 0);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();

			// Blend
			Backend.BeginPass(BlendRenderTarget, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
			Backend.BeginInstance(BlendShader[index].Handle, new int[] { EdgeRenderTarget.Textures[0].Handle, AreaTexture.Handle, SearchTexture.Handle },
				samplers: new int[] { LinearClampSampler, LinearClampSampler, NearClampSampler });
			Backend.BindShaderVariable(BlendParams[index].SamplerEdge, 0);
			Backend.BindShaderVariable(BlendParams[index].SamplerArea, 1);
			Backend.BindShaderVariable(BlendParams[index].SamplerSearch, 2);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();

			// Neighborhood
			Backend.BeginPass(output, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
			Backend.BeginInstance(NeighborhoodShader[index].Handle, new int[] { input.Textures[0].Handle, BlendRenderTarget.Textures[0].Handle },
				samplers: new int[] { LinearClampSampler, LinearClampSampler });
			Backend.BindShaderVariable(NeighborhoodParams[index].SamplerScene, 0);
			Backend.BindShaderVariable(NeighborhoodParams[index].SamplerBlend, 1);

			Backend.DrawMesh(QuadMesh.MeshHandle);
			Backend.EndPass();
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
