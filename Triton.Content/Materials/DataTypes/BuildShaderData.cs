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

		public BuildShaderData(string value)
		{
			Value = value;
		}
	}
}
