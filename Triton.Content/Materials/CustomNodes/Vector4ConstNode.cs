using NodeGraphControl;
using NodeGraphControl.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Content.Materials.CustomNodes
{
	public class Vector4ConstNode : NodeGraphNode
	{
		public Vector4 Value { get; set; }

		public Vector4ConstNode(XmlTreeNode p_TreeNode, NodeGraphView p_View)
			: base(p_TreeNode, p_View)
		{
			Setup();
			Value = Common.StringConverter.Parse<Vector4>(p_TreeNode.m_attributes["Value"]);
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
			if (connectorIndex > 0)
			{
				var outputVar = Context.NextVariable("f");
				float value = 0;

				switch (connectorIndex)
				{
					case 1: value = Value.X; break;
					case 2: value = Value.Y; break;
					case 3: value = Value.Z; break;
					case 4: value = Value.W; break;
					default: break;
				}

				var statements = new List<string>()
				{
					string.Format("float {0} = {1}", outputVar, Common.StringConverter.ToString(value)),
				};

				return new DataTypes.ShaderData(statements, outputVar);
			}
			else
			{
				var outputVar = Context.NextVariable("v");

				var statements = new List<string>()
				{
					string.Format("vec4 {0} = vec3({1}, {2}, {3}, {4})", outputVar, Common.StringConverter.ToString(Value.X), Common.StringConverter.ToString(Value.Y), Common.StringConverter.ToString(Value.Z), Common.StringConverter.ToString(Value.W)),
				};

				return new DataTypes.ShaderData(statements, outputVar);
			}
		}

		protected override string GetName()
		{
			return "Vector4: " + Value.ToString();
		}

		public override XmlTreeNode SerializeToXML(XmlTreeNode p_Parent)
		{
			var element = base.SerializeToXML(p_Parent);
			element.AddParameter("Value", Common.StringConverter.ToString(Value));

			return element;
		}
	}
}
