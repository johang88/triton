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
		public Attrib[] Attribs { get; internal set; }

		public ShaderProgram(string name, string parameters)
			: base(name, parameters)
		{
			Handle = -1;
		}

		public struct Attrib
		{
			public AttribType Type;
			public string Name;
		}

		public enum AttribType
		{
			Position = 0,
			Normal = 1,
			Tangent = 2,
			TexCoord = 3
		}
	}
}
