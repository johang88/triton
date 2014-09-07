using NodeGraphControl;
using NodeGraphControl.Xml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Content.Materials.CustomNodes
{
	public class FloatConstNode : NodeGraphNode
	{
		public float Value { get; set; }

		public FloatConstNode(XmlTreeNode p_TreeNode, NodeGraphView p_View)
			: base(p_TreeNode, p_View)
		{
			Value = float.Parse(p_TreeNode.m_attributes["Value"]);
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
			var outputVar = Context.NextVariable("f");

			var statements = new List<string>()
			{
				string.Format("float {0} = {1}", outputVar, Common.StringConverter.ToString(Value)),
			};

			return new DataTypes.ShaderData(statements, outputVar);
		}

		protected override string GetName()
		{
			return "Float: " + Value.ToString();
		}

		public override XmlTreeNode SerializeToXML(XmlTreeNode p_Parent)
		{
			var element = base.SerializeToXML(p_Parent);
			element.AddParameter("Value", Value.ToString());

			return element;
		}
	}
}
