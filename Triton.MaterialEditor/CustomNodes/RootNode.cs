using NodeGraphControl;
using NodeGraphControl.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.MaterialEditor.CustomNodes
{
	public class RootNode : NodeGraphNode
	{
		public RootNode(int p_X, int p_Y, NodeGraphView p_View)
			: base(p_X, p_Y, p_View, false)
		{
			Setup();
		}

		public RootNode(XmlTreeNode p_TreeNode, NodeGraphView p_View)
			: base(p_TreeNode, p_View)
		{
			Setup();
		}

		private void Setup()
		{
			this.m_sName = "Material";
			this.m_Connectors.Add(new NodeGraphConnector("Diffuse", this, ConnectorType.InputConnector, 0, "Vector4"));
			this.m_Connectors.Add(new NodeGraphConnector("Normals", this, ConnectorType.InputConnector, 1, "Vector3"));
			this.m_Connectors.Add(new NodeGraphConnector("Metallic", this, ConnectorType.InputConnector, 2, "Float"));
			this.m_Connectors.Add(new NodeGraphConnector("Roughness", this, ConnectorType.InputConnector, 3, "Float"));
			this.Height = 96;
		}
	}
}
