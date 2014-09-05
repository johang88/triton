using NodeGraphControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.MaterialEditor.DataTypes
{
	public class ShaderData : NodeGraphData
	{
		public string Value { get; set; }

		public ShaderData(string value)
		{
			Value = value;
		}
	}
}
