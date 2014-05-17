using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Triton.Graphics.Resources
{
	public class ShaderProgram : Triton.Common.Resource
	{
		public int Handle { get; internal set; }
		public Dictionary<Common.HashedString, int> Uniforms = new Dictionary<Common.HashedString, int>();
		private Dictionary<Common.HashedString, Common.HashedString> BindNamesToVarNames = new Dictionary<Common.HashedString, Common.HashedString>();
		private readonly Backend Backend;

		public ShaderProgram(string name, string parameters, Backend backend)
			: base(name, parameters)
		{
			Handle = -1;
			Backend = backend;
		}

		internal void Reset()
		{
			Uniforms = new Dictionary<Common.HashedString, int>();
			BindNamesToVarNames = new Dictionary<Common.HashedString, Common.HashedString>();
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

		public void GetUniformLocations<T>(T handles)
		{
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
}
