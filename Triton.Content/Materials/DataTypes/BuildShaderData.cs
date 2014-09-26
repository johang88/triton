using NodeGraphControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Content.Materials.DataTypes
{
	public class BuildShaderData : NodeGraphData
	{
		public string Value { get; set; }
		public List<string> Defines { get; set; }

		public BuildShaderData(string value, List<string> defines)
		{
			Value = value;
			Defines = defines;
		}
	}
}
