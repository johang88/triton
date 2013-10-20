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
		internal ServiceStack.Text.JsonObject Definition;

		public Material(string name, string parameters)
			: base(name, parameters)
		{
		}

		public virtual void Initialize()
		{
			Initialized = true;
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
		public virtual void BindMaterial(Backend backend, ref Matrix4 world, ref Matrix4 worldView, ref Matrix4 itWorldView, ref Matrix4 modelViewProjection)
		{
			if (!Initialized)
			{
				Initialize();
			}
		}
	}
}
