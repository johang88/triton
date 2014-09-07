using NodeGraphControl;
using NodeGraphControl.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Content.Materials.CustomNodes
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
			m_sName = "Normal map";
			this.Height = 64;

			this.m_Connectors.Add(new NodeGraphConnector("Texture", this, ConnectorType.InputConnector, 0, "Vector4"));
			m_Connectors.Add(new NodeGraphConnector("Normals", this, ConnectorType.OutputConnector, 0, "Vector3"));
		}

		public override NodeGraphData Process(int connectorIndex)
		{
			var a = Connectors[0].Process() as DataTypes.ShaderData;
			
			var inputVar = a.VarName;
			var outputVar = Context.NextVariable("normals");
			var tbnVar = Context.NextVariable("tbn");

			var statements = new List<string>()
			{
				string.Format("mat3x3 {0} = mat3x3(normalize(tangent), normalize(bitangent), normalize(normal))", tbnVar),
				string.Format("vec3 {0} = normalize({1} * normalize({2}.xyz * 2.0 - 1.0))", outputVar, tbnVar, inputVar)
			};

			return new DataTypes.ShaderData(a.Statements.Concat(statements).ToList(), outputVar);
		}
	}
}
