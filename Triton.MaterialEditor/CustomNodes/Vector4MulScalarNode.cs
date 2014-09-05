using NodeGraphControl;
using NodeGraphControl.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.MaterialEditor.CustomNodes
{
	public class Vector4MulScalarNode : NodeGraphNode
	{
		public Vector4MulScalarNode(XmlTreeNode p_TreeNode, NodeGraphView p_View)
			: base(p_TreeNode, p_View)
		{
			Setup();
		}

		public Vector4MulScalarNode(int p_X, int p_Y, NodeGraphView p_View, bool p_CanBeSelected)
			: base(p_X, p_Y, p_View, p_CanBeSelected)
		{
			Setup();
		}

		private void Setup()
		{
			m_sName = "A*B(vec4*float)";

			this.m_Connectors.Add(new NodeGraphConnector("A", this, ConnectorType.InputConnector, 0, "Vector4"));
			this.m_Connectors.Add(new NodeGraphConnector("B", this, ConnectorType.InputConnector, 1, "Float"));
			this.m_Connectors.Add(new NodeGraphConnector("Result (A+B)", this, ConnectorType.OutputConnector, 0, "Vector4"));
			this.Height = 64;
		}

		public override NodeGraphData Process(int connectorIndex)
		{
			var a = Connectors[0].Process() as DataTypes.ShaderData;
			var b = Connectors[1].Process() as DataTypes.ShaderData;

			var outputVar = Context.NextVariable("v");

			var statements = new List<string>()
			{
				string.Format("vec4 {0} = {1} * {2}", outputVar, a.VarName, b.VarName),
			};

			return new DataTypes.ShaderData(a.Statements.Concat(b.Statements).Concat(statements).ToList(), outputVar);
		}

	}
}
