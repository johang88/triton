using NodeGraphControl;
using NodeGraphControl.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.MaterialEditor.CustomNodes
{
	public class Vector4ConstNode : NodeGraphNode
	{
		public Vector4 Value { get; set; }

		public Vector4ConstNode(XmlTreeNode p_TreeNode, NodeGraphView p_View)
			: base(p_TreeNode, p_View)
		{
			Setup();
		}

		public Vector4ConstNode(int p_X, int p_Y, NodeGraphView p_View, bool p_CanBeSelected)
			: base(p_X, p_Y, p_View, p_CanBeSelected)
		{
			Setup();
		}

		private void Setup()
		{
			m_sName = "Vector4: 0.0f";
			Value = Vector4.Zero;

			Width = 150;
			Height = 116;

			m_Connectors.Add(new NodeGraphConnector("Value", this, ConnectorType.OutputConnector, 0, "Vector4"));
			m_Connectors.Add(new NodeGraphConnector("X", this, ConnectorType.OutputConnector, 1, "Float"));
			m_Connectors.Add(new NodeGraphConnector("Y", this, ConnectorType.OutputConnector, 2, "Float"));
			m_Connectors.Add(new NodeGraphConnector("Z", this, ConnectorType.OutputConnector, 3, "Float"));
			m_Connectors.Add(new NodeGraphConnector("W", this, ConnectorType.OutputConnector, 4, "Float"));
		}

		public override NodeGraphData Process(int connectorIndex)
		{
			switch (connectorIndex)
			{
				case 1: return new DataTypes.ShaderData(Common.StringConverter.ToString(Value.X));
				case 2: return new DataTypes.ShaderData(Common.StringConverter.ToString(Value.Y));
				case 3: return new DataTypes.ShaderData(Common.StringConverter.ToString(Value.Z));
				case 4: return new DataTypes.ShaderData(Common.StringConverter.ToString(Value.W));
				default: return new DataTypes.ShaderData(Common.StringConverter.ToString(Value));
			}
		}

		protected override string GetName()
		{
			return "Vector4: " + Value.ToString();
		}
	}
}
