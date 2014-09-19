using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Resources.Materials
{
	public class StandardMaterial : Material
	{
		public readonly Dictionary<string, Texture> Textures = new Dictionary<string, Texture>();

		public ShaderProgram Shader;
		private ShaderHandles Handles;
		private Common.ResourceManager ResourceManager;
		public bool IsSkinned;

		private int[] TextureHandles;
		private int[] SamplerToTexture;
		private int[] Samplers;

		public StandardMaterial(string name, string parameters, Common.ResourceManager resourceManager, bool isSkinned)
			: base(name, parameters)
		{
			IsSkinned = isSkinned;
			ResourceManager = resourceManager;
		}

		public override void Initialize(Backend backend)
		{
			base.Initialize(backend);

			Handles = new ShaderHandles();
			Shader.GetUniformLocations(Handles);

			TextureHandles = new int[Textures.Count];
			SamplerToTexture = new int[Textures.Count];
			Samplers = new int[Textures.Count];

			var i = 0;
			foreach (var samplerInfo in Textures)
			{
				TextureHandles[i] = samplerInfo.Value.Handle;
				Samplers[i] = backend.DefaultSampler;
				SamplerToTexture[i] = Shader.GetUniform(samplerInfo.Key);
				i++;
			}
		}

		public override void Unload()
		{
			base.Unload();

			foreach (var samplerInfo in Textures)
			{
				ResourceManager.Unload(samplerInfo.Value);
			}

			Textures.Clear();
		}

		public override void BindMaterial(Backend backend, Camera camera, ref Matrix4 world, ref Matrix4 worldView, ref Matrix4 itWorld, ref Matrix4 modelViewProjection, SkeletalAnimation.SkeletonInstance skeleton, int renderStateId)
		{
			base.BindMaterial(backend, camera, ref world, ref worldView, ref itWorld, ref modelViewProjection, skeleton, renderStateId);

			backend.BeginInstance(Shader.Handle, TextureHandles, samplers: Samplers, renderStateId: renderStateId);

			backend.BindShaderVariable(Handles.ModelViewProjection, ref modelViewProjection);
			backend.BindShaderVariable(Handles.World, ref world);
			backend.BindShaderVariable(Handles.WorldView, ref worldView);
			backend.BindShaderVariable(Handles.ItWorld, ref itWorld);

			for (var i = 0; i < SamplerToTexture.Length; i++)
			{
				backend.BindShaderVariable(SamplerToTexture[i], i);
			}

			backend.BindShaderVariable(Handles.Time, backend.ElapsedTime);
			backend.BindShaderVariable(Handles.CameraPosition, ref camera.Position);

			if (skeleton != null)
			{
				backend.BindShaderVariable(Handles.Bones, ref skeleton.FinalBoneTransforms);
			}
		}

		class ShaderHandles
		{
			public int ModelViewProjection = 0;
			public int World = 0;
			public int WorldView = 0;
			public int ItWorldView = 0;
			public int Bones = 0;
			public int CameraPosition = 0;
			public int ItWorld = 0;
			public int Time = 0;
		}
	}
}
