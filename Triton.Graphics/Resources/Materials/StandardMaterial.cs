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

		public StandardMaterial(string name, string parameters)
			: base(name, parameters)
		{
		}

		public override void Initialize()
		{
			base.Initialize();

			Handles = new ShaderHandles();
			Shader.GetUniformLocations(Handles);
		}

		public override void BindMaterial(Backend backend, ref Matrix4 world, ref Matrix4 worldView, ref Matrix4 itWorldView, ref Matrix4 modelViewProjection)
		{
			base.BindMaterial(backend, ref world, ref worldView, ref itWorldView, ref modelViewProjection);

			backend.BeginInstance(Shader.Handle, new int[] { Diffuse.Handle, Normal.Handle, Specular.Handle });

			backend.BindShaderVariable(Handles.ModelViewProjection, ref modelViewProjection);
			backend.BindShaderVariable(Handles.HandleWorld, ref world);
			backend.BindShaderVariable(Handles.HandleWorldView, ref worldView);
			backend.BindShaderVariable(Handles.HandleITWorldView, ref itWorldView);
			backend.BindShaderVariable(Handles.HandleDiffuseTexture, 0);
			backend.BindShaderVariable(Handles.HandleNormalMap, 1);
			backend.BindShaderVariable(Handles.HandleSpecularMap, 2);
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
		}
	}
}
