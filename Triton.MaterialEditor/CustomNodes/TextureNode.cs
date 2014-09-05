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
		static int Counter = 0;
		private int SamplerNumber = Counter++;
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

		public override NodeGraphData Process(int connectorIndex)
		{
			var samplerName = "sampler_" + Common.StringConverter.ToString(SamplerNumber);
			var sample = "texture(" + samplerName + ", texCoord)";
			string outputVar = "";

			if (connectorIndex > 0)
			{
				outputVar = Context.NextVariable("f");
				switch (connectorIndex)
				{
					case 1: sample += ".x";  break;
					case 2: sample += ".y"; break;
					case 3: sample += ".z"; break;
					case 4: sample += ".w"; break;
					default: break;
				}
			}
			else
			{
				outputVar = Context.NextVariable("v");
			}
			
			var statements = new List<string>()
			{
				string.Format("vec4 {0} = {1}", outputVar, sample),
			};

			return new DataTypes.ShaderData(statements, outputVar);
		}
	}
}
