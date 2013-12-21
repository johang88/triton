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
		public Texture Gloss;
		public Texture Specular;
		public ShaderProgram Shader;
		private ShaderHandles Handles;
		private Common.ResourceManager ResourceManager;
		public bool IsSkinned;

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
		}

		public override void Unload()
		{
			base.Unload();

			if (Diffuse != null)
				ResourceManager.Unload(Diffuse);
			if (Normal != null)
				ResourceManager.Unload(Normal);
			if (Gloss != null)
				ResourceManager.Unload(Gloss);
			if (Specular != null)
				ResourceManager.Unload(Specular);
			if (Shader != null)
				ResourceManager.Unload(Shader);

			Diffuse = null;
			Normal = null;
			Gloss = null;
			Specular = null;
			Shader = null;
		}

		public override void BindMaterial(Backend backend, ref Matrix4 world, ref Matrix4 worldView, ref Matrix4 itWorldView, ref Matrix4 modelViewProjection, SkeletalAnimation.SkeletonInstance skeleton)
		{
			base.BindMaterial(backend, ref world, ref worldView, ref itWorldView, ref modelViewProjection, skeleton);

			backend.BeginInstance(Shader.Handle, new int[] { Diffuse.Handle, Normal.Handle, Specular.Handle });

			backend.BindShaderVariable(Handles.ModelViewProjection, ref modelViewProjection);
			backend.BindShaderVariable(Handles.HandleWorld, ref world);
			backend.BindShaderVariable(Handles.HandleWorldView, ref worldView);
			backend.BindShaderVariable(Handles.HandleITWorldView, ref itWorldView);
			backend.BindShaderVariable(Handles.HandleDiffuseTexture, 0);
			backend.BindShaderVariable(Handles.HandleNormalMap, 1);
			backend.BindShaderVariable(Handles.HandleSpecularMap, 2);

			if (skeleton != null)
			{
				backend.BindShaderVariable(Handles.HandleBones, ref skeleton.FinalBoneTransforms);
			}
		}

		class ShaderHandles
		{
			public int ModelViewProjection = 0;
			public int HandleWorld = 0;
			public int HandleWorldView = 0;
			public int HandleITWorldView = 0;
			public int HandleDiffuseTexture = 0;
			public int HandleNormalMap = 0;
			public int HandleSpecularMap = 0;
			public int HandleBones = 0;
		}
	}
}
