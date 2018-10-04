using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Triton.Graphics.Resources
{
    public class ShaderProgram
    {
        public int Handle { get; internal set; }
        public Dictionary<Common.HashedString, int> Uniforms = new Dictionary<Common.HashedString, int>();
        private Dictionary<Common.HashedString, Common.HashedString> _bindNamesToVarNames = new Dictionary<Common.HashedString, Common.HashedString>();
        private readonly Backend _backend;

        public bool HasTeselation = false;

        private object _mutex = new object();
        private readonly List<object> _boundHandles = new List<object>();

        public ShaderProgram(Backend backend)
        {
            Handle = -1;
            _backend = backend;
        }

        internal void Reset()
        {
            Uniforms = new Dictionary<Common.HashedString, int>();
            _bindNamesToVarNames = new Dictionary<Common.HashedString, Common.HashedString>();
        }

        public int GetUniform(Common.HashedString name)
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
