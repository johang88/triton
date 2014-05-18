using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Resources.Materials
{
	public class SplatMaterial : Material
	{
		public Texture Diffuse1;
		public Texture Diffuse2;
		public Texture Diffuse3;
		public Texture Diffuse4;
		public Texture Normal1;
		public Texture Normal2;
		public Texture Normal3;
		public Texture Normal4;
		public Texture Splat;

		public ShaderProgram Shader;
		private ShaderHandles Handles;
		private Common.ResourceManager ResourceManager;

		public SplatMaterial(string name, string parameters, Common.ResourceManager resourceManager)
			: base(name, parameters)
		{
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
			if (Splat != null)
				ResourceManager.Unload(Splat);

			Diffuse1 = null;
			Diffuse2 = null;
			Diffuse3 = null;
			Diffuse4 = null;
			Normal1 = null;
			Normal2 = null;
			Normal3 = null;
			Normal4 = null;
			Splat = null;
		}

		public override void BindMaterial(Backend backend, ref Matrix4 world, ref Matrix4 worldView, ref Matrix4 itWorldView, ref Matrix4 modelViewProjection, SkeletalAnimation.SkeletonInstance skeleton)
		{
			base.BindMaterial(backend, ref world, ref worldView, ref itWorldView, ref modelViewProjection, skeleton);

			backend.BeginInstance(Shader.Handle, 
				new int[] { Splat.Handle, Diffuse1.Handle, Diffuse2.Handle, Diffuse3.Handle, Diffuse4.Handle, Normal1.Handle, Normal3.Handle, Normal3.Handle, Normal4.Handle }, 
				null);

			backend.BindShaderVariable(Handles.ModelViewProjection, ref modelViewProjection);
			backend.BindShaderVariable(Handles.HandleWorld, ref world);
			backend.BindShaderVariable(Handles.HandleWorldView, ref worldView);
			backend.BindShaderVariable(Handles.HandleITWorldView, ref itWorldView);
			backend.BindShaderVariable(Handles.HandleSplatTexture, 0);
			backend.BindShaderVariable(Handles.HandleDiffuse1, 1);
			backend.BindShaderVariable(Handles.HandleDiffuse2, 2);
			backend.BindShaderVariable(Handles.HandleDiffuse3, 3);
			backend.BindShaderVariable(Handles.HandleDiffuse4, 4);
			backend.BindShaderVariable(Handles.HandleNormalMap1, 5);
			backend.BindShaderVariable(Handles.HandleNormalMap2, 6);
			backend.BindShaderVariable(Handles.HandleNormalMap3, 7);
			backend.BindShaderVariable(Handles.HandleNormalMap4, 8);
		}

		class ShaderHandles
		{
			public int ModelViewProjection = 0;
			public int HandleWorld = 0;
			public int HandleWorldView = 0;
			public int HandleITWorldView = 0;
			public int HandleSplatTexture = 0;
			public int HandleDiffuse1 = 0;
			public int HandleDiffuse2 = 0;
			public int HandleDiffuse3 = 0;
			public int HandleDiffuse4 = 0;
			public int HandleNormalMap1 = 0;
			public int HandleNormalMap2 = 0;
			public int HandleNormalMap3 = 0;
			public int HandleNormalMap4 = 0;
		}
	}
}
