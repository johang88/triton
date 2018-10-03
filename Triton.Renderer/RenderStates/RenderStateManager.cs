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
        private readonly List<StateData> _states = new List<StateData>();
        private int _currentStateIndex = -1;

        private bool _currentDepthMask = true;
        private bool _depthMaskToRestore = true;

        public RenderStateManager()
        {
        }

        public int CreateRenderState(bool enableAlphaBlend = false, bool enableDepthWrite = true, bool enableDepthTest = true, BlendingFactorSrc src = BlendingFactorSrc.Zero, BlendingFactorDest dest = BlendingFactorDest.One, CullFaceMode cullFaceMode = CullFaceMode.Back, bool enableCullFace = true, DepthFunction depthFunction = DepthFunction.Less, bool wireFrame = false, bool scissorTest = false)
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
                DepthFunction = depthFunction,
                WireFrame = wireFrame,
                ScissorTest = scissorTest
            };

            if (_states.Contains(stateData))
            {
                return _states.IndexOf(stateData);
            }

            _states.Add(stateData);
            return _states.Count - 1;
        }

        public void EnableTemporaryDepthMask()
        {
            _depthMaskToRestore = _currentDepthMask;

            if (!_currentDepthMask)
            {
                GL.DepthMask(true);
            }
        }

        public void RestoreDepthMask()
        {
            if (!_depthMaskToRestore)
            {
                GL.DepthMask(false);
            }
        }

        public void ApplyRenderState(int id)
        {
            if (_currentStateIndex == id || id >= _states.Count)
                return;

            StateData oldState = _currentStateIndex >= 0 ? _states[_currentStateIndex] : null;

            _currentStateIndex = id;
            var state = _states[_currentStateIndex];

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
                _currentDepthMask = state.DepthWrite;
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

            if (oldState == null || oldState.WireFrame != state.WireFrame)
            {
                GL.PolygonMode(MaterialFace.FrontAndBack, state.WireFrame ? PolygonMode.Line : PolygonMode.Fill);
            }

            if (oldState == null || oldState.ScissorTest != state.ScissorTest)
            {
                if (state.ScissorTest)
                    GL.Enable(EnableCap.ScissorTest);
                else
                    GL.Disable(EnableCap.ScissorTest);
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
            public bool WireFrame = false;
            public bool ScissorTest = false;

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
                    && s.DepthFunction == DepthFunction
                    && s.WireFrame == WireFrame
                    && s.ScissorTest == ScissorTest;
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
                    && s.DepthFunction == DepthFunction
                    && s.WireFrame == WireFrame
                    && s.ScissorTest == ScissorTest;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }
    }
}
