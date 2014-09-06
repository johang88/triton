using NodeGraphControl;
using NodeGraphControl.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.MaterialEditor.DataTypes;

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
			this.m_Connectors.Add(new NodeGraphConnector("Specular", this, ConnectorType.InputConnector, 4, "Float"));
			this.Height = 116;
		}

		private string BuildShader(ShaderData shaderData)
		{
			var builder = new StringBuilder();

			foreach (var statement in shaderData.Statements)
			{
				builder.Append(statement).Append(";\n");
			}

			builder.Append("return ").Append(shaderData.VarName).Append(";\n");
			var diffuseShader = builder.ToString();

			return builder.ToString();
		}

		public override NodeGraphData Process(int connectorIndex)
		{
			NodeGraphListData inputData = this.GetInputData();

			string diffuseShader = "return vec4(0.5, 0.5, 0.5, 1);";
			if (inputData.Data[0] is ShaderData)
			{
				diffuseShader = BuildShader(inputData.Data[0] as DataTypes.ShaderData);
			}

			string normalsShader = "return normalize(normal);";
			if (inputData.Data[1] is ShaderData)
			{
				normalsShader = BuildShader(inputData.Data[1] as DataTypes.ShaderData);
			}

			string metallicShader = "return 0.5;";
			if (inputData.Data[2] is ShaderData)
			{
				metallicShader = BuildShader(inputData.Data[2] as DataTypes.ShaderData);
			}

			string roughnessShader = "return 0.5;";
			if (inputData.Data[3] is ShaderData)
			{
				roughnessShader = BuildShader(inputData.Data[3] as DataTypes.ShaderData);
			}

			string specularShader = "return 0.5;";
			if (inputData.Data[4] is ShaderData)
			{
				specularShader = BuildShader(inputData.Data[4] as DataTypes.ShaderData);
			}

			var shaderBuilder = new StringBuilder();

			shaderBuilder
				.AppendLine("vec4 get_diffuse() {")
				.AppendLine(diffuseShader)
				.AppendLine("}")
				.AppendLine("vec3 get_normals() {")
				.AppendLine(normalsShader)
				.AppendLine("}")
				.AppendLine("float get_metallic() {")
				.AppendLine(metallicShader)
				.AppendLine("}")
				.AppendLine("float get_roughness() {")
				.AppendLine(roughnessShader)
				.AppendLine("}")
				.AppendLine("float get_specular() {")
				.AppendLine(specularShader)
				.AppendLine("}");

			return new BuildShaderData(shaderBuilder.ToString());
		}
	}
}
