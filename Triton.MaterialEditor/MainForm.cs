using NodeGraphControl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Triton.Content.Materials.CustomNodes;
using Triton.Content.Materials.DataTypes;
using Triton.Content.Materials.NodeGraphDataTypes;

namespace Triton.MaterialEditor
{
	public partial class MainForm : Form
	{
		private RootNode RootNode;
		private Point MouseLocation = Point.Empty;

		public MainForm()
		{
			InitializeComponent();

			Tools.CustomTypeDescriptorProvider.Register(typeof(Vector3ConstNode));
			Tools.CustomTypeDescriptorProvider.Register(typeof(Vector4ConstNode));

			nodeGraphPanel1.View.RegisterDataType(new NodeGraphDataTypeFloat());
			nodeGraphPanel1.View.RegisterDataType(new NodeGraphDataTypeVector3());
			nodeGraphPanel1.View.RegisterDataType(new NodeGraphDataTypeVector4());

			RootNode = new RootNode(100, 0, nodeGraphPanel1.View);
			nodeGraphPanel1.AddNode(RootNode);
		}

		private void nodeGraphPanel1_MouseMove(object sender, MouseEventArgs e)
		{
			MouseLocation = e.Location;
		}

		private void processToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var result = RootNode.Process(0) as BuildShaderData;
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
			nodeGraphPanel1.AddNode(new FloatConstNode(v_ViewPos.X, v_ViewPos.Y, nodeGraphPanel1.View, true));
		}

		private void vector3ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Point v_ViewPos = nodeGraphPanel1.ControlToView(MouseLocation);
			nodeGraphPanel1.AddNode(new Vector3ConstNode(v_ViewPos.X, v_ViewPos.Y, nodeGraphPanel1.View, true));
		}

		private void vector4ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Point v_ViewPos = nodeGraphPanel1.ControlToView(MouseLocation);
			nodeGraphPanel1.AddNode(new Vector4ConstNode(v_ViewPos.X, v_ViewPos.Y, nodeGraphPanel1.View, true));
		}

		private void textureToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Point v_ViewPos = nodeGraphPanel1.ControlToView(MouseLocation);
			nodeGraphPanel1.AddNode(new TextureConstNode(v_ViewPos.X, v_ViewPos.Y, nodeGraphPanel1.View, true));
		}

		private void normalMapToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Point v_ViewPos = nodeGraphPanel1.ControlToView(MouseLocation);
			nodeGraphPanel1.AddNode(new NormalMapNode(v_ViewPos.X, v_ViewPos.Y, nodeGraphPanel1.View, true));
		}

		private void addToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Point v_ViewPos = nodeGraphPanel1.ControlToView(MouseLocation);
			nodeGraphPanel1.AddNode(new Vector4AddNode(v_ViewPos.X, v_ViewPos.Y, nodeGraphPanel1.View, true));
		}

		private void multiplyScalarToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Point v_ViewPos = nodeGraphPanel1.ControlToView(MouseLocation);
			nodeGraphPanel1.AddNode(new Vector4MulScalarNode(v_ViewPos.X, v_ViewPos.Y, nodeGraphPanel1.View, true));
		}

		private void loadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				nodeGraphPanel1.LoadCurrentView(openFileDialog1.FileName);
			}

			foreach (NodeGraphNode i_Node in this.nodeGraphPanel1.View.NodeCollection)
			{
				if (i_Node is RootNode)
				{
					RootNode = i_Node as RootNode;
				}
			}
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				if (System.IO.File.Exists(saveFileDialog1.FileName))
				{
					if (MessageBox.Show(saveFileDialog1.FileName + " Exists, overwrite?", "Overwrite Confirmation", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Cancel)
					{
						return;
					}
				}

				nodeGraphPanel1.SaveCurrentView(saveFileDialog1.FileName);
			}
		}
	}
}
