using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;

namespace Triton.Renderer.RenderStates
{
	class RenderStateManager
	{
		const int MaxHandles = 128;
		private readonly List<StateData> States = new List<StateData>();
		private int CurrentStateIndex = -1;

		public RenderStateManager()
		{
		}

		public int CreateRenderState(bool enableAlphaBlend = false, bool enableDepthWrite = true, bool enableDepthTest = true, BlendingFactorSrc src = BlendingFactorSrc.Zero, BlendingFactorDest dest = BlendingFactorDest.One, CullFaceMode cullFaceMode = CullFaceMode.Back, bool enableCullFace = true, DepthFunction depthFunction = DepthFunction.Less)
		{
			var stateData = new StateData
			{
				AlphaBlend = enableAlphaBlend,
				DepthWrite = enableDepthWrite,
				DepthTest = enableDepthTest,
				BlendingSrc = src,
				BlendingDest = dest,
				CullFace = cullFaceMode,
				EnableCullFace = enableCullFace,
				DepthFunction = depthFunction
			};

			if (States.Contains(stateData))
			{
				return States.IndexOf(stateData);
			}

			States.Add(stateData);
			return States.Count - 1;
		}

		public void ApplyRenderState(int id)
		{
			if (CurrentStateIndex == id || id >= States.Count)
				return;

			StateData oldState = CurrentStateIndex >= 0 ? States[CurrentStateIndex] : null;
			
			CurrentStateIndex = id;
			var state = States[CurrentStateIndex];

			if (oldState == null || oldState.AlphaBlend != state.AlphaBlend)
			{
				if (state.AlphaBlend)
					GL.Enable(EnableCap.Blend);
				else
					GL.Disable(EnableCap.Blend);
			}

			if (oldState == null || oldState.DepthTest != state.DepthTest)
			{
				if (state.DepthTest)
					GL.Enable(EnableCap.DepthTest);
				else
					GL.Disable(EnableCap.DepthTest);
			}

			if (oldState == null || oldState.DepthWrite != state.DepthWrite)
			{
				GL.DepthMask(state.DepthWrite);
			}

			if (oldState == null || oldState.EnableCullFace != state.EnableCullFace)
			{
				if (state.EnableCullFace)
					GL.Enable(EnableCap.CullFace);
				else
					GL.Disable(EnableCap.CullFace);
			}

			if (oldState == null || (oldState.BlendingSrc != state.BlendingSrc || oldState.BlendingDest != state.BlendingDest))
			{
				GL.BlendFunc((OpenTK.Graphics.OpenGL.BlendingFactor)(int)state.BlendingSrc, (OpenTK.Graphics.OpenGL.BlendingFactor)(int)state.BlendingDest);
			}

			if (oldState == null || oldState.CullFace != state.CullFace)
			{
				GL.CullFace((OpenTK.Graphics.OpenGL.CullFaceMode)(int)state.CullFace);
			}

			if (oldState == null || oldState.DepthFunction != state.DepthFunction)
			{
				GL.DepthFunc((OpenTK.Graphics.OpenGL.DepthFunction)(int)state.DepthFunction);
			}
		}

		class StateData : IEquatable<StateData>
		{
			public bool AlphaBlend = false;
			public bool DepthWrite = true;
			public bool DepthTest = true;
			public BlendingFactorSrc BlendingSrc = BlendingFactorSrc.Zero;
			public BlendingFactorDest BlendingDest = BlendingFactorDest.One;
			public CullFaceMode CullFace = CullFaceMode.Back;
			public bool EnableCullFace = true;
			public DepthFunction DepthFunction = DepthFunction.Less;

			public override bool Equals(object obj)
			{
				if (obj == null)
					return false;

				if (!(obj is StateData))
					return false;

				var s = obj as StateData;
				return s.AlphaBlend == AlphaBlend
					&& s.DepthWrite == DepthWrite
					&& s.DepthTest == DepthTest
					&& s.BlendingSrc == BlendingSrc
					&& s.BlendingDest == BlendingDest
					&& s.CullFace == CullFace
					&& s.EnableCullFace == EnableCullFace
					&& s.DepthFunction == DepthFunction;
			}

			public bool Equals(StateData s)
			{
				return s.AlphaBlend == AlphaBlend
					&& s.DepthWrite == DepthWrite
					&& s.DepthTest == DepthTest
					&& s.BlendingSrc == BlendingSrc
					&& s.BlendingDest == BlendingDest
					&& s.CullFace == CullFace
					&& s.EnableCullFace == EnableCullFace
					&& s.DepthFunction == DepthFunction;
			}

			public override int GetHashCode()
			{
				return base.GetHashCode();
			}
		}
	}
}
