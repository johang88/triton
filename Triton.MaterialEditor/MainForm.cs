using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Triton.MaterialEditor
{
	public partial class MainForm : Form
	{
		private CustomNodes.RootNode RootNode;
		private Point MouseLocation = Point.Empty;

		public MainForm()
		{
			InitializeComponent();

			Tools.CustomTypeDescriptorProvider.Register(typeof(CustomNodes.Vector3ConstNode));
			Tools.CustomTypeDescriptorProvider.Register(typeof(CustomNodes.Vector4ConstNode));

			this.nodeGraphPanel1.View.RegisterDataType(new NodeGraphDataTypes.NodeGraphDataTypeFloat());
			this.nodeGraphPanel1.View.RegisterDataType(new NodeGraphDataTypes.NodeGraphDataTypeVector3());
			this.nodeGraphPanel1.View.RegisterDataType(new NodeGraphDataTypes.NodeGraphDataTypeVector4());

			RootNode = new CustomNodes.RootNode(100, 0, nodeGraphPanel1.View);
			this.nodeGraphPanel1.AddNode(RootNode);
		}

		private void nodeGraphPanel1_MouseMove(object sender, MouseEventArgs e)
		{
			MouseLocation = e.Location;
		}

		private void processToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var result = RootNode.Process(0) as DataTypes.BuildShaderData;

		}

		private void nodeGraphPanel1_onSelectionChanged(object sender, NodeGraphControl.NodeGraphPanelSelectionEventArgs args)
		{
			if (args.NewSelectionCount == 1)
			{
				propertyGrid1.SelectedObject = nodeGraphPanel1.View.SelectedItems[0];
			}
		}

		private void nodeGraphPanel1_onSelectionCleared(object sender, NodeGraphControl.NodeGraphPanelSelectionEventArgs args)
		{
			propertyGrid1.SelectedObject = null;
		}

		private void floatToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Point v_ViewPos = nodeGraphPanel1.ControlToView(MouseLocation);
			this.nodeGraphPanel1.AddNode(new CustomNodes.FloatConstNode(v_ViewPos.X, v_ViewPos.Y, nodeGraphPanel1.View, true));
		}

		private void vector3ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Point v_ViewPos = nodeGraphPanel1.ControlToView(MouseLocation);
			this.nodeGraphPanel1.AddNode(new CustomNodes.Vector3ConstNode(v_ViewPos.X, v_ViewPos.Y, nodeGraphPanel1.View, true));
		}

		private void vector4ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Point v_ViewPos = nodeGraphPanel1.ControlToView(MouseLocation);
			this.nodeGraphPanel1.AddNode(new CustomNodes.Vector4ConstNode(v_ViewPos.X, v_ViewPos.Y, nodeGraphPanel1.View, true));
		}

		private void textureToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Point v_ViewPos = nodeGraphPanel1.ControlToView(MouseLocation);
			this.nodeGraphPanel1.AddNode(new CustomNodes.TextureConstNode(v_ViewPos.X, v_ViewPos.Y, nodeGraphPanel1.View, true));
		}

		private void normalMapToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Point v_ViewPos = nodeGraphPanel1.ControlToView(MouseLocation);
			this.nodeGraphPanel1.AddNode(new CustomNodes.NormalMapNode(v_ViewPos.X, v_ViewPos.Y, nodeGraphPanel1.View, true));
		}

		private void addToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Point v_ViewPos = nodeGraphPanel1.ControlToView(MouseLocation);
			this.nodeGraphPanel1.AddNode(new CustomNodes.Vector4AddNode(v_ViewPos.X, v_ViewPos.Y, nodeGraphPanel1.View, true));
		}

		private void multiplyScalarToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Point v_ViewPos = nodeGraphPanel1.ControlToView(MouseLocation);
			this.nodeGraphPanel1.AddNode(new CustomNodes.Vector4MulScalarNode(v_ViewPos.X, v_ViewPos.Y, nodeGraphPanel1.View, true));
		}
	}
}
