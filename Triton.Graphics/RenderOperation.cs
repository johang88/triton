using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics
{
	public struct RenderOperation
	{
		/// <summary>
		/// Mesh to render
		/// </summary>
		public int MeshHandle;

		/// <summary>
		/// World matrix to use
		/// </summary>
		public Matrix4 WorldMatrix;

		public Resources.Material Material;

		/// <summary>
		/// Attached skeleton, can be null
		/// </summary>
		public SkeletalAnimation.SkeletonInstance Skeleton;

		/// <summary>
		/// Set to true if object is to be rendered using instancing, used to select correct shadow shaders etc
		/// </summary>
		public bool UseInstancing;
	}
}
