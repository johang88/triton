using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Resources
{
	public class Material
	{
		private bool Initialized = false;

		public readonly Dictionary<string, Texture> Textures = new Dictionary<string, Texture>();

		public ShaderProgram Shader;
		private ShaderHandles Handles;
		private Common.ResourceManager ResourceManager;
		public bool IsSkinned;

		private int[] TextureHandles;
		private int[] SamplerToTexture;
		private int[] Samplers;

		// Not an awesome solution
		private static int LastId = 0;
		public readonly int Id = LastId++;

		public Material(Common.ResourceManager resourceManager, bool isSkinned)
		{
			IsSkinned = isSkinned;
			ResourceManager = resourceManager;
        }

		public void Initialize(Backend backend)
		{
			Initialized = true;

			Handles = new ShaderHandles();
			Shader.BindUniformLocations(Handles);

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

		public virtual void Unload()
		{
			foreach (var samplerInfo in Textures)
			{
				ResourceManager.Unload(samplerInfo.Value);
			}

			Textures.Clear();
		}

		public void BeginInstance(Backend backend, Camera camera, int renderStateId)
		{
			if (!Initialized)
			{
				Initialize(backend);
			}

			backend.BeginInstance(Shader.Handle, TextureHandles, samplers: Samplers, renderStateId: renderStateId);
			for (var i = 0; i < SamplerToTexture.Length; i++)
			{
				backend.BindShaderVariable(SamplerToTexture[i], i);
			}

			backend.BindShaderVariable(Handles.Time, backend.ElapsedTime);
			backend.BindShaderVariable(Handles.CameraPosition, ref camera.Position);
		}

		/// <summary>
		/// Bind the material, this will call BeginInstance on the backend
		/// It is up to the caller to call EndInstance
		/// </summary>
		/// <param name="backend"></param>
		/// <param name="world"></param>
		/// <param name="worldView"></param>
		/// <param name="itWorldView"></param>
		/// <param name="modelViewProjection"></param>
		public void BindPerObject(Backend backend, ref Matrix4 world, ref Matrix4 worldView, ref Matrix4 itWorld, ref Matrix4 modelViewProjection, SkeletalAnimation.SkeletonInstance skeleton)
		{
			backend.BindShaderVariable(Handles.ModelViewProjection, ref modelViewProjection);
			backend.BindShaderVariable(Handles.World, ref world);
			backend.BindShaderVariable(Handles.WorldView, ref worldView);
			backend.BindShaderVariable(Handles.ItWorld, ref itWorld);

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
