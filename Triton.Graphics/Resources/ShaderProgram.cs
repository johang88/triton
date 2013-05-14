using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Resources
{
	public class ShaderProgram : Triton.Common.Resource
	{
		public int Handle { get; internal set; }
		private readonly Dictionary<Triton.Common.HashedString, int> Uniforms = new Dictionary<Common.HashedString, int>();
		private readonly Dictionary<string, string> BindNamesToVarNames = new Dictionary<string, string>();
		private readonly Backend Backend;

		public ShaderProgram(string name, string parameters, Backend backend)
			: base(name, parameters)
		{
			Handle = -1;
			Backend = backend;
		}

		public int GetUniform(Triton.Common.HashedString name)
		{
			int uniformLocation;
			if (!Uniforms.TryGetValue(name, out uniformLocation))
			{
				uniformLocation = Backend.RenderSystem.GetUniformLocation(Handle, Triton.Common.HashedStringTable.GetString(name));
				Uniforms.Add(name, uniformLocation);
			}

			return uniformLocation;
		}

		public void AddUniform(string bindName, string name)
		{
			BindNamesToVarNames.Add(bindName, name);
		}

		/// <summary>
		/// Get a list of key-value-pairs of external uniform names to internal uniform names
		/// Ie:
		/// uniform(vec3, lightDir, LightDirection) will result in ("LightDirection", "lightDir")
		/// 
		/// This can be used to automatically resolve uniform locations and to keep the actual variable
		/// names in the shader independant of the data that they should be bound to.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<KeyValuePair<string, string>> GetUniforms()
		{
			return BindNamesToVarNames;
		}
	}
}
