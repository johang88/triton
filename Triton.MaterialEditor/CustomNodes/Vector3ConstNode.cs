using NodeGraphControl;
using NodeGraphControl.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.MaterialEditor.CustomNodes
{
	public class Vector3ConstNode : NodeGraphNode
	{
		public Vector3 Value { get; set; }

		public Vector3ConstNode(XmlTreeNode p_TreeNode, NodeGraphView p_View)
			: base(p_TreeNode, p_View)
		{
			Setup();
		}

		public Vector3ConstNode(int p_X, int p_Y, NodeGraphView p_View, bool p_CanBeSelected)
			: base(p_X, p_Y, p_View, p_CanBeSelected)
		{
			Setup();
		}

		private void Setup()
		{
			m_sName = "Vector3: 0.0f";
			Value = Vector3.Zero;

			Width = 150;
			Height = 96;

			m_Connectors.Add(new NodeGraphConnector("Value", this, ConnectorType.OutputConnector, 0, "Vector3"));
			m_Connectors.Add(new NodeGraphConnector("X", this, ConnectorType.OutputConnector, 1, "Float"));
			m_Connectors.Add(new NodeGraphConnector("Y", this, ConnectorType.OutputConnector, 2, "Float"));
			m_Connectors.Add(new NodeGraphConnector("Z", this, ConnectorType.OutputConnector, 3, "Float"));
		}

		protected override string GetName()
		{
			return "Vector3: " + Value.ToString();
		}
	}
}
