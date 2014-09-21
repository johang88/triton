using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Resources
{
	public class Mesh : Triton.Common.Resource
	{
		public SubMesh[] SubMeshes { get; internal set; }
		public float BoundingSphereRadius;
		private bool IsLoadedCache = false;

		public Mesh(string name, string parameters)
			: base(name, parameters)
		{
			SubMeshes = new SubMesh[0];
		}

		public bool IsLoaded()
		{
			if (IsLoadedCache)
				return true;

			if (State != Common.ResourceLoadingState.Loaded)
				return false;

			foreach (var subMesh in SubMeshes)
			{
				if (!subMesh.Material.IsLoaded())
					return false;
			}

			IsLoadedCache = true;

			return true;
		}
	}

	public class SubMesh
	{
		public Material Material;
		public float BoundingSphereRadius;

		public int VertexBufferHandle;
		public int IndexBufferHandle;
		public int Handle;
	}
}
