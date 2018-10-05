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
		private ShaderHandles _handles;
		private Common.ResourceManager _resourceManager;
		public bool IsSkinned;

		private int[] _textureHandles;
		private int[] _samplerToTexture;
		private int[] _samplers;

		// Not an awesome solution
		private static int LastId = 0;
		public readonly int Id = LastId++;

		public Material(Common.ResourceManager resourceManager)
		{
			_resourceManager = resourceManager;
        }

		public void Initialize(Backend backend)
		{
			Initialized = true;

			_handles = new ShaderHandles();
			Shader.BindUniformLocations(_handles);

			_textureHandles = new int[Textures.Count];
			_samplerToTexture = new int[Textures.Count];
			_samplers = new int[Textures.Count];

			var i = 0;
			foreach (var samplerInfo in Textures)
			{
				_textureHandles[i] = samplerInfo.Value.Handle;
				_samplers[i] = backend.DefaultSampler;
				_samplerToTexture[i] = Shader.GetUniform(samplerInfo.Key);
				i++;
			}
		}

		public virtual void Unload()
		{
			foreach (var samplerInfo in Textures)
			{
				_resourceManager.Unload(samplerInfo.Value);
			}

			Textures.Clear();
		}

		public void BeginInstance(Backend backend, Camera camera, int renderStateId)
		{
			if (!Initialized)
			{
				Initialize(backend);
			}

			backend.BeginInstance(Shader.Handle, _textureHandles, samplers: _samplers, renderStateId: renderStateId);
			for (var i = 0; i < _samplerToTexture.Length; i++)
			{
				backend.BindShaderVariable(_samplerToTexture[i], i);
			}

			backend.BindShaderVariable(_handles.Time, backend.ElapsedTime);
			backend.BindShaderVariable(_handles.CameraPosition, ref camera.Position);
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
			backend.BindShaderVariable(_handles.ModelViewProjection, ref modelViewProjection);
			backend.BindShaderVariable(_handles.World, ref world);
			backend.BindShaderVariable(_handles.WorldView, ref worldView);
			backend.BindShaderVariable(_handles.ItWorld, ref itWorld);

			if (skeleton != null)
			{
				backend.BindShaderVariable(_handles.Bones, ref skeleton.FinalBoneTransforms);
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
