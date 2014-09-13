using NodeGraphControl;
using NodeGraphControl.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Content.Materials.CustomNodes
{
	public class FloatLerpNode : NodeGraphNode
	{
		public FloatLerpNode(XmlTreeNode p_TreeNode, NodeGraphView p_View)
			: base(p_TreeNode, p_View)
		{
			Setup();
		}

		public FloatLerpNode(int p_X, int p_Y, NodeGraphView p_View, bool p_CanBeSelected)
			: base(p_X, p_Y, p_View, p_CanBeSelected)
		{
			Setup();
		}

		private void Setup()
		{
			m_sName = "lerp(float)";

			this.m_Connectors.Add(new NodeGraphConnector("X", this, ConnectorType.InputConnector, 0, "Float"));
			this.m_Connectors.Add(new NodeGraphConnector("Y", this, ConnectorType.InputConnector, 1, "Float"));
			this.m_Connectors.Add(new NodeGraphConnector("A", this, ConnectorType.InputConnector, 2, "Float"));
			this.m_Connectors.Add(new NodeGraphConnector("Result", this, ConnectorType.OutputConnector, 0, "Float"));
			this.Height = 92;
		}

		public override NodeGraphData Process(int connectorIndex)
		{
			var x = Connectors[0].Process() as DataTypes.ShaderData;
			var y = Connectors[1].Process() as DataTypes.ShaderData;
			var a = Connectors[2].Process() as DataTypes.ShaderData;

			var outputVar = Context.NextVariable("v");

			var statements = new List<string>()
			{
				string.Format("float {0} = mix({1}, {2}, {3})", outputVar, x.VarName, y.VarName, a.VarName),
			};

			return new DataTypes.ShaderData(x.Statements.Concat(y.Statements).Concat(a.Statements).Concat(statements).ToList(), outputVar);
		}
	}
}
