using NodeGraphControl;
using NodeGraphControl.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.MaterialEditor.CustomNodes
{
	public class NormalMapNode : NodeGraphNode
	{
		public string Value { get; set; }

		public NormalMapNode(XmlTreeNode p_TreeNode, NodeGraphView p_View)
			: base(p_TreeNode, p_View)
		{
			Setup();
		}

		public NormalMapNode(int p_X, int p_Y, NodeGraphView p_View, bool p_CanBeSelected)
			: base(p_X, p_Y, p_View, p_CanBeSelected)
		{
			Setup();
		}

		private void Setup()
		{
			m_sName = "Texture: ...";
			Value = "/no_normal_map";

			Width = 210;
			Height = 96;

			m_Connectors.Add(new NodeGraphConnector("Value", this, ConnectorType.OutputConnector, 0, "Vector3"));
			m_Connectors.Add(new NodeGraphConnector("X", this, ConnectorType.OutputConnector, 1, "Float"));
			m_Connectors.Add(new NodeGraphConnector("Y", this, ConnectorType.OutputConnector, 2, "Float"));
			m_Connectors.Add(new NodeGraphConnector("Z", this, ConnectorType.OutputConnector, 3, "Float"));
		}

		public override NodeGraphData Process(int connectorIndex)
		{
			var shader = "texture(samplerNormal, texCoord)";
			switch (connectorIndex)
			{
				case 1: shader += ".x"; break;
				case 2: shader += ".y"; break;
				case 3: shader += ".z"; break;
				default: break;
			}

			return new DataTypes.ShaderData(shader);
		}

		protected override string GetName()
		{
			return "Texture: " + Value;
		}
	}
}
