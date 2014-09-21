using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Resources
{
	public class Material : Triton.Common.Resource
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

		public Material(string name, string parameters, Common.ResourceManager resourceManager, bool isSkinned)
			: base(name, parameters)
		{
			IsSkinned = isSkinned;
			ResourceManager = resourceManager;
		}

		public void Initialize(Backend backend)
		{
			Initialized = true;

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

		public virtual void Unload()
		{
			foreach (var samplerInfo in Textures)
			{
				ResourceManager.Unload(samplerInfo.Value);
			}

			Textures.Clear();
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
		public void BindMaterial(Backend backend, Camera camera, ref Matrix4 world, ref Matrix4 worldView, ref Matrix4 itWorld, ref Matrix4 modelViewProjection, SkeletalAnimation.SkeletonInstance skeleton, int renderStateId)
		{
			if (!Initialized)
			{
				Initialize(backend);
			}

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

		public bool IsLoaded()
		{
			if (State != Common.ResourceLoadingState.Loaded)
				return false;

			if (Shader.State != Common.ResourceLoadingState.Loaded)
				return false;
			
			foreach (var texture in Textures)
			{
				if (texture.Value.State != Common.ResourceLoadingState.Loaded)
					return false;
			}

			return true;
		}
	}
}
