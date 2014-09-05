using NodeGraphControl;
using NodeGraphControl.Xml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.MaterialEditor.CustomNodes
{
	public class FloatConstNode : NodeGraphNode
	{
		public float Value { get; set; }

		public FloatConstNode(XmlTreeNode p_TreeNode, NodeGraphView p_View)
			: base(p_TreeNode, p_View)
		{
			Setup();
		}

		public FloatConstNode(int p_X, int p_Y, NodeGraphView p_View, bool p_CanBeSelected)
			: base(p_X, p_Y, p_View, p_CanBeSelected)
		{
			Setup();
		}

		private void Setup()
		{
			m_sName = "Float: 0.0f";
			Value = 0;

			Width = 80;
			Height = 45;

			m_Connectors.Add(new NodeGraphConnector("Value", this, ConnectorType.OutputConnector, 0, "Float"));
		}

		public override NodeGraphData Process(int connectorIndex)
		{
			return new DataTypes.ShaderData(Common.StringConverter.ToString(Value));
		}

		protected override string GetName()
		{
			return "Float: " + Value.ToString();
		}
	}
}
