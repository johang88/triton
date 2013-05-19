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
		private Dictionary<Common.HashedString, int> Uniforms = new Dictionary<Common.HashedString, int>();
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
				uniformLocation = Backend.RenderSystem.GetUniformLocation(Handle, Triton.Common.HashedStringTable.GetString(name));
				Uniforms.Add(name, uniformLocation);
			}

			return uniformLocation;
		}

		public int GetAliasedUniform(Common.HashedString name)
		{
			return GetUniform(BindNamesToVarNames[name]);
		}

		public bool HasAliasedUniform(Common.HashedString name)
		{
			return BindNamesToVarNames.ContainsKey(name);
		}

		public void AddUniform(string bindName, string name)
		{
			BindNamesToVarNames.Add(bindName, name);
		}

	}
}
