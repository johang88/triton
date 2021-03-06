﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Triton.Utility;

namespace Triton.Graphics.Resources
{
    public class ShaderProgram : IDisposable
    {
        public int Handle { get; internal set; }
        public Dictionary<Utility.HashedString, int> Uniforms = new Dictionary<Utility.HashedString, int>();
        private Dictionary<Utility.HashedString, Utility.HashedString> _bindNamesToVarNames = new Dictionary<Utility.HashedString, Utility.HashedString>();
        private readonly Backend _backend;

        public bool HasTeselation = false;

        private object _mutex = new object();
        private readonly List<object> _boundHandles = new List<object>();

        public ShaderProgram(Backend backend)
        {
            Handle = -1;
            _backend = backend;
        }

        public void Dispose()
        {
            if (Handle >= 0)
            {
                _backend.RenderSystem.DestroyShader(Handle);
                Handle = -1;
            }
        }

        internal void Reset()
        {
            Uniforms = new Dictionary<Utility.HashedString, int>();
            _bindNamesToVarNames = new Dictionary<Utility.HashedString, HashedString>();
        }

        public int GetUniform(Utility.HashedString name)
        {
            int uniformLocation;
            if (!Uniforms.TryGetValue(name, out uniformLocation))
            {
                return -1;
            }

            return uniformLocation;
        }

        internal void RefreshBoundUniforms()
        {
            lock (_mutex)
            {
                foreach (var handles in _boundHandles)
                {
                    var type = handles.GetType();
                    foreach (var field in type.GetFields())
                    {
                        if (field.FieldType != typeof(int))
                            continue;

                        var fieldName = field.Name;
                        var uniformName = fieldName.Replace("Handle", "");
                        uniformName = char.ToLower(uniformName[0]) + uniformName.Substring(1);

                        int uniformLocation = GetUniform(uniformName);

                        field.SetValue(handles, uniformLocation);
                    }
                }
            }
        }

        public void BindUniformLocations<T>(T handles) where T : class
        {
            lock (_mutex)
            {
                if (!_boundHandles.Contains(handles))
                    _boundHandles.Add(handles);

                var type = typeof(T);
                foreach (var field in type.GetFields())
                {
                    if (field.FieldType != typeof(int))
                        continue;

                    var fieldName = field.Name;
                    var uniformName = fieldName.Replace("Handle", "");
                    uniformName = char.ToLower(uniformName[0]) + uniformName.Substring(1);

                    int uniformLocation = GetUniform(uniformName);

                    field.SetValue(handles, uniformLocation);
                }
            }
        }

        public void UnbindUniformLocations<T>(T handles)
        {
            lock (_mutex)
            {
                _boundHandles.Remove(handles);
            }
        }
    }
}
