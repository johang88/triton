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

		// Not an awesome solution
		private static int LastId = 0;
		public readonly int Id = LastId++;

		public Material(string name, string parameters)
			: base(name, parameters)
		{
		}

		public virtual void Initialize(Backend backend)
		{
			Initialized = true;
		}

		public virtual void Unload()
		{
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
		public virtual void BindMaterial(Backend backend, Camera camera, ref Matrix4 world, ref Matrix4 worldView, ref Matrix4 itWorld, ref Matrix4 modelViewProjection, SkeletalAnimation.SkeletonInstance skeleton, int renderStateId)
		{
			if (!Initialized)
			{
				Initialize(backend);
			}
		}
	}
}
