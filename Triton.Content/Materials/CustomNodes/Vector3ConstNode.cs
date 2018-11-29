using NodeGraphControl;
using NodeGraphControl.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Utility;

namespace Triton.Content.Materials.CustomNodes
{
	public class Vector3ConstNode : NodeGraphNode
	{
		public Vector3 Value { get; set; }

		public Vector3ConstNode(XmlTreeNode p_TreeNode, NodeGraphView p_View)
			: base(p_TreeNode, p_View)
		{
			Setup();
			Value = StringConverter.Parse<Vector3>(p_TreeNode.m_attributes["Value"]);
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
					default: break;
				}

				var statements = new List<string>()
				{
					string.Format("float {0} = {1}", outputVar, StringConverter.ToString(value)),
				};

				return new DataTypes.ShaderData(statements, outputVar);
			}
			else
			{
				var outputVar = Context.NextVariable("v");

				var statements = new List<string>()
				{
					string.Format("vec3 {0} = vec3({1}, {2}, {3})", outputVar, StringConverter.ToString(Value.X), StringConverter.ToString(Value.Y), StringConverter.ToString(Value.Z)),
				};

				return new DataTypes.ShaderData(statements, outputVar);
			}
		}

		protected override string GetName()
		{
			return "Vector3: " + Value.ToString();
		}

		public override XmlTreeNode SerializeToXML(XmlTreeNode p_Parent)
		{
			var element = base.SerializeToXML(p_Parent);
			element.AddParameter("Value", StringConverter.ToString(Value));

			return element;
		}
	}
}
