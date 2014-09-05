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

		public override NodeGraphData Process(int connectorIndex)
		{
			NodeGraphListData inputData = this.GetInputData();

			var diffuseShaderData = (inputData.Data[0] as DataTypes.ShaderData);
			var builder = new StringBuilder();

			foreach (var statement in diffuseShaderData.Statements)
			{
				builder.Append(statement).Append(";\n");
			}

			builder.Append("return ").Append(diffuseShaderData.VarName).Append(";\n");
			var diffuseShader = builder.ToString();

			//var normalsShader = (inputData.Data[1] as DataTypes.ShaderData).Value;
			//var metallicShader = (inputData.Data[2] as DataTypes.ShaderData).Value;
			//var roughnessShader = (inputData.Data[3] as DataTypes.ShaderData).Value;

			return null;
		}
	}
}
