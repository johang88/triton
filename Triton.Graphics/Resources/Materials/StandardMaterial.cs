using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Resources.Materials
{
	public class StandardMaterial : Material
	{
		public Texture Diffuse;
		public Texture Normal;
		public Texture Roughness;
		public Texture DiffuseCube;
		public Vector3 DiffuseColor;
		public float MetallicValue;
		public float SpecularValue;
		public float RoughnessValue;

		public ShaderProgram Shader;
		private ShaderHandles Handles;
		private Common.ResourceManager ResourceManager;
		public bool IsSkinned;

		private int[] Textures;
		private int[] Samplers;

		public StandardMaterial(string name, string parameters, Common.ResourceManager resourceManager, bool isSkinned)
			: base(name, parameters)
		{
			IsSkinned = isSkinned;
			ResourceManager = resourceManager;
		}

		public override void Initialize()
		{
			base.Initialize();

			Handles = new ShaderHandles();
			Shader.GetUniformLocations(Handles);

			var textures = new List<int>();
			if (Diffuse != null)
				textures.Add(Diffuse.Handle);
			if (DiffuseCube != null)
				textures.Add(DiffuseCube.Handle);
			if (Normal != null)
				textures.Add(Normal.Handle);
			if (Roughness != null)
				textures.Add(Roughness.Handle);


			Textures = textures.ToArray();
		}

		public override void Unload()
		{
			base.Unload();

			if (Diffuse != null)
				ResourceManager.Unload(Diffuse);
			if (Normal != null)
				ResourceManager.Unload(Normal);
			if (Roughness != null)
				ResourceManager.Unload(Roughness);
			if (Shader != null)
				ResourceManager.Unload(Shader);

			Diffuse = null;
			Normal = null;
			Roughness = null;
			Shader = null;
		}

		public override void BindMaterial(Backend backend, ref Matrix4 world, ref Matrix4 worldView, ref Matrix4 itWorldView, ref Matrix4 modelViewProjection, SkeletalAnimation.SkeletonInstance skeleton)
		{
			base.BindMaterial(backend, ref world, ref worldView, ref itWorldView, ref modelViewProjection, skeleton);

			var itWorld = Matrix4.Transpose(Matrix4.Invert(world));

			if (Samplers == null)
				Samplers = new int[] { backend.DefaultSampler, backend.DefaultSampler, backend.DefaultSampler, backend.DefaultSampler };

			backend.BeginInstance(Shader.Handle, Textures, samplers: Samplers);

			backend.BindShaderVariable(Handles.ModelViewProjection, ref modelViewProjection);
			backend.BindShaderVariable(Handles.World, ref world);
			backend.BindShaderVariable(Handles.WorldView, ref worldView);
			backend.BindShaderVariable(Handles.ItWorldView, ref itWorldView);
			backend.BindShaderVariable(Handles.ItWorld, ref itWorld);

			var textureUnit = 0;
			if (Diffuse != null)
				backend.BindShaderVariable(Handles.SamplerDiffuse, textureUnit++);
			if (DiffuseCube != null)
				backend.BindShaderVariable(Handles.SamplerDiffuseCube, textureUnit++);
			if (Normal != null)
				backend.BindShaderVariable(Handles.SamplerNormal, textureUnit++);
			if (Roughness != null)
				backend.BindShaderVariable(Handles.SamplerRoughness, textureUnit++);
			backend.BindShaderVariable(Handles.SamplerEnvironment, textureUnit++);

			backend.BindShaderVariable(Handles.MaterialDiffuseColor, ref DiffuseColor);
			backend.BindShaderVariable(Handles.MaterialMetallicValue, MetallicValue);
			backend.BindShaderVariable(Handles.MaterialSpecularValue, SpecularValue);
			backend.BindShaderVariable(Handles.MaterialRoughnessValue, RoughnessValue);

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
			public int SamplerDiffuse = 0;
			public int SamplerDiffuseCube = 0;
			public int SamplerNormal = 0;
			public int SamplerRoughness = 0;
			public int SamplerEnvironment = 0;
			public int Bones = 0;
			public int MaterialDiffuseColor = 0;
			public int MaterialMetallicValue = 0;
			public int MaterialSpecularValue = 0;
			public int MaterialRoughnessValue = 0;
			public int CameraPosition = 0;
			public int ItWorld = 0;
		}
	}
}
