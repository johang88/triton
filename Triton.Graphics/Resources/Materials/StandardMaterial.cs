using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Resources.Materials
{
	public class StandardMaterial : Material
	{
		public Texture Splat;
		public Texture Diffuse1;
		public Texture Diffuse2;
		public Texture Diffuse3;
		public Texture Diffuse4;
		public Texture Normal1;
		public Texture Normal2;
		public Texture Normal3;
		public Texture Normal4;
		public Texture Roughness;
		public Texture DiffuseCube;
		public Vector3 DiffuseColor;
		public float MetallicValue;
		public float SpecularValue;
		public float RoughnessValue;
		public Vector2 UvAnimation;

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
			if (Splat != null)
				textures.Add(Splat.Handle);
			if (Diffuse1 != null)
				textures.Add(Diffuse1.Handle);
			if (Diffuse2 != null)
				textures.Add(Diffuse2.Handle);
			if (Diffuse3 != null)
				textures.Add(Diffuse3.Handle);
			if (Diffuse4 != null)
				textures.Add(Diffuse4.Handle);
			if (DiffuseCube != null)
				textures.Add(DiffuseCube.Handle);
			if (Normal1 != null)
				textures.Add(Normal1.Handle);
			if (Normal2 != null)
				textures.Add(Normal2.Handle);
			if (Normal3 != null)
				textures.Add(Normal3.Handle);
			if (Normal4 != null)
				textures.Add(Normal4.Handle);
			if (Roughness != null)
				textures.Add(Roughness.Handle);

			textures.Add(0); // env ambient
			textures.Add(0); // env spec

			Textures = textures.ToArray();
		}

		public override void Unload()
		{
			base.Unload();

			if (Splat!= null)
				ResourceManager.Unload(Splat);
			if (Diffuse1 != null)
				ResourceManager.Unload(Diffuse1);
			if (Diffuse2 != null)
				ResourceManager.Unload(Diffuse2);
			if (Diffuse3 != null)
				ResourceManager.Unload(Diffuse3);
			if (Diffuse4 != null)
				ResourceManager.Unload(Diffuse4);
			if (Normal1 != null)
				ResourceManager.Unload(Normal1);
			if (Normal2 != null)
				ResourceManager.Unload(Normal2);
			if (Normal3 != null)
				ResourceManager.Unload(Normal3);
			if (Normal4 != null)
				ResourceManager.Unload(Normal4);
			if (Roughness != null)
				ResourceManager.Unload(Roughness);
			if (Shader != null)
				ResourceManager.Unload(Shader);

			Splat = null;
			Diffuse1 = null;
			Diffuse2 = null;
			Diffuse3 = null;
			Diffuse4 = null;
			Normal1 = null;
			Normal2 = null;
			Normal3 = null;
			Normal4 = null;
			Roughness = null;
			Shader = null;
		}

		public override void BindMaterial(Backend backend, Texture environmentMap, Texture environmentMapSpecular, Camera camera, ref Matrix4 world, ref Matrix4 worldView, ref Matrix4 itWorldView, ref Matrix4 modelViewProjection, SkeletalAnimation.SkeletonInstance skeleton, int renderStateId)
		{
			base.BindMaterial(backend, environmentMap, environmentMapSpecular, camera, ref world, ref worldView, ref itWorldView, ref modelViewProjection, skeleton, renderStateId);

			var itWorld = Matrix4.Transpose(Matrix4.Invert(world));

			if (Samplers == null)
			{
				Samplers = new int[Textures.Length];
				for (var i = 0; i < Samplers.Length; i++)
				{
					Samplers[i] = backend.DefaultSampler;
				}
			}
			
			Textures[Textures.Length - 2] = environmentMap.Handle;
			Textures[Textures.Length - 1] = environmentMapSpecular.Handle;
			backend.BeginInstance(Shader.Handle, Textures, samplers: Samplers, renderStateId: renderStateId);

			backend.BindShaderVariable(Handles.ModelViewProjection, ref modelViewProjection);
			backend.BindShaderVariable(Handles.World, ref world);
			backend.BindShaderVariable(Handles.WorldView, ref worldView);
			backend.BindShaderVariable(Handles.ItWorldView, ref itWorldView);
			backend.BindShaderVariable(Handles.ItWorld, ref itWorld);

			var textureUnit = 0;
			if (Splat != null)
				backend.BindShaderVariable(Handles.SamplerSplat, textureUnit++);
			if (Diffuse1 != null)
				backend.BindShaderVariable(Handles.SamplerDiffuse1, textureUnit++);
			if (Diffuse2 != null)
				backend.BindShaderVariable(Handles.SamplerDiffuse2, textureUnit++);
			if (Diffuse3 != null)
				backend.BindShaderVariable(Handles.SamplerDiffuse3, textureUnit++);
			if (Diffuse4 != null)
				backend.BindShaderVariable(Handles.SamplerDiffuse4, textureUnit++);
			if (DiffuseCube != null)
				backend.BindShaderVariable(Handles.SamplerDiffuseCube, textureUnit++);
			if (Normal1 != null)
				backend.BindShaderVariable(Handles.SamplerNormal1, textureUnit++);
			if (Normal2 != null)
				backend.BindShaderVariable(Handles.SamplerNormal2, textureUnit++);
			if (Normal3 != null)
				backend.BindShaderVariable(Handles.SamplerNormal3, textureUnit++);
			if (Normal4 != null)
				backend.BindShaderVariable(Handles.SamplerNormal4, textureUnit++);
			if (Roughness != null)
				backend.BindShaderVariable(Handles.SamplerRoughness, textureUnit++);
			backend.BindShaderVariable(Handles.SamplerEnvironment, textureUnit++);
			backend.BindShaderVariable(Handles.SamplerEnvironmentSpec, textureUnit++);

			backend.BindShaderVariable(Handles.MaterialDiffuseColor, ref DiffuseColor);
			backend.BindShaderVariable(Handles.MaterialMetallicValue, MetallicValue);
			backend.BindShaderVariable(Handles.MaterialSpecularValue, SpecularValue);
			backend.BindShaderVariable(Handles.MaterialRoughnessValue, RoughnessValue);
			backend.BindShaderVariable(Handles.Time, backend.ElapsedTime);
			backend.BindShaderVariable(Handles.UvAnimation, ref UvAnimation);
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
			public int SamplerSplat = 0;
			public int SamplerDiffuse1 = 0;
			public int SamplerDiffuse2 = 0;
			public int SamplerDiffuse3 = 0;
			public int SamplerDiffuse4 = 0;
			public int SamplerDiffuseCube = 0;
			public int SamplerNormal1 = 0;
			public int SamplerNormal2 = 0;
			public int SamplerNormal3 = 0;
			public int SamplerNormal4 = 0;
			public int SamplerRoughness = 0;
			public int SamplerEnvironment = 0;
			public int SamplerEnvironmentSpec = 0;
			public int Bones = 0;
			public int MaterialDiffuseColor = 0;
			public int MaterialMetallicValue = 0;
			public int MaterialSpecularValue = 0;
			public int MaterialRoughnessValue = 0;
			public int CameraPosition = 0;
			public int ItWorld = 0;
			public int Time = 0;
			public int UvAnimation = 0;
		}
	}
}
