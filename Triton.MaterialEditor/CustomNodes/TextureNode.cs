using NodeGraphControl;
using NodeGraphControl.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.MaterialEditor.CustomNodes
{
	public class TextureConstNode : NodeGraphNode
	{
		public string Value { get; set; }

		public TextureConstNode(XmlTreeNode p_TreeNode, NodeGraphView p_View)
			: base(p_TreeNode, p_View)
		{
			Setup();
		}

		public TextureConstNode(int p_X, int p_Y, NodeGraphView p_View, bool p_CanBeSelected)
			: base(p_X, p_Y, p_View, p_CanBeSelected)
		{
			Setup();
		}

		private void Setup()
		{
			m_sName = "Texture: ...";
			Value = "/no_texture";

			Width = 210;
			Height = 116;

			m_Connectors.Add(new NodeGraphConnector("Value", this, ConnectorType.OutputConnector, 0, "Vector4"));
			m_Connectors.Add(new NodeGraphConnector("R", this, ConnectorType.OutputConnector, 1, "Float"));
			m_Connectors.Add(new NodeGraphConnector("G", this, ConnectorType.OutputConnector, 2, "Float"));
			m_Connectors.Add(new NodeGraphConnector("B", this, ConnectorType.OutputConnector, 3, "Float"));
			m_Connectors.Add(new NodeGraphConnector("A", this, ConnectorType.OutputConnector, 4, "Float"));
		}

		protected override string GetName()
		{
			return "Texture: " + Value;
		}
	}
}
