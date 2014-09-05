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
		public List<string> Statements = new List<string>();
		public string VarName { get; set; }

		public ShaderData(List<string> statements, string varName)
		{
			Statements = statements;
			VarName = varName;
		}
	}
}
