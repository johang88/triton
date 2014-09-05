namespace Triton.MaterialEditor
{
	partial class MainForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.nodeGraphPanel1 = new NodeGraphControl.NodeGraphPanel();
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.floatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.vector3ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.vector4ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.addToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.multiplyScalarToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.textureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.normalMapToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.processToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.contextMenuStrip1.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// nodeGraphPanel1
			// 
			this.nodeGraphPanel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(67)))), ((int)(((byte)(65)))), ((int)(((byte)(64)))));
			this.nodeGraphPanel1.ConnectorFillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
			this.nodeGraphPanel1.ConnectorFillSelectedColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
			this.nodeGraphPanel1.ConnectorHitZoneBleed = 2;
			this.nodeGraphPanel1.ConnectorOutlineColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
			this.nodeGraphPanel1.ConnectorOutlineSelectedColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
			this.nodeGraphPanel1.ConnectorTextColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
			this.nodeGraphPanel1.ContextMenuStrip = this.contextMenuStrip1;
			this.nodeGraphPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.nodeGraphPanel1.DrawShadow = true;
			this.nodeGraphPanel1.EnableDrawDebug = false;
			this.nodeGraphPanel1.GridAlpha = ((byte)(16));
			this.nodeGraphPanel1.GridPadding = 256;
			this.nodeGraphPanel1.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
			this.nodeGraphPanel1.LinkEditableColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(255)))), ((int)(((byte)(0)))));
			this.nodeGraphPanel1.LinkHardness = 2F;
			this.nodeGraphPanel1.LinkVisualStyle = NodeGraphControl.LinkVisualStyle.Curve;
			this.nodeGraphPanel1.Location = new System.Drawing.Point(0, 0);
			this.nodeGraphPanel1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.nodeGraphPanel1.Name = "nodeGraphPanel1";
			this.nodeGraphPanel1.NodeConnectorFont = new System.Drawing.Font("Tahoma", 7F);
			this.nodeGraphPanel1.NodeConnectorTextZoomTreshold = 0.8F;
			this.nodeGraphPanel1.NodeFillColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
			this.nodeGraphPanel1.NodeFillSelectedColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(128)))), ((int)(((byte)(100)))));
			this.nodeGraphPanel1.NodeHeaderColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
			this.nodeGraphPanel1.NodeHeaderSize = 24;
			this.nodeGraphPanel1.NodeOutlineColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
			this.nodeGraphPanel1.NodeOutlineSelectedColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(160)))), ((int)(((byte)(128)))));
			this.nodeGraphPanel1.NodeScaledConnectorFont = new System.Drawing.Font("Tahoma", 7F);
			this.nodeGraphPanel1.NodeScaledTitleFont = new System.Drawing.Font("Tahoma", 8F);
			this.nodeGraphPanel1.NodeSignalInvalidColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
			this.nodeGraphPanel1.NodeSignalValidColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(255)))), ((int)(((byte)(0)))));
			this.nodeGraphPanel1.NodeTextColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
			this.nodeGraphPanel1.NodeTextShadowColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
			this.nodeGraphPanel1.NodeTitleFont = new System.Drawing.Font("Tahoma", 8F);
			this.nodeGraphPanel1.NodeTitleZoomThreshold = 0.5F;
			this.nodeGraphPanel1.SelectionFillColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(128)))), ((int)(((byte)(90)))), ((int)(((byte)(30)))));
			this.nodeGraphPanel1.SelectionOutlineColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(180)))), ((int)(((byte)(60)))));
			this.nodeGraphPanel1.ShowGrid = true;
			this.nodeGraphPanel1.Size = new System.Drawing.Size(1200, 781);
			this.nodeGraphPanel1.SmoothBehavior = false;
			this.nodeGraphPanel1.TabIndex = 4;
			this.nodeGraphPanel1.UseLinkColoring = true;
			this.nodeGraphPanel1.onSelectionChanged += new NodeGraphControl.NodeGraphPanelSelectionEventHandler(this.nodeGraphPanel1_onSelectionChanged);
			this.nodeGraphPanel1.onSelectionCleared += new NodeGraphControl.NodeGraphPanelSelectionEventHandler(this.nodeGraphPanel1_onSelectionCleared);
			this.nodeGraphPanel1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.nodeGraphPanel1_MouseMove);
			// 
			// contextMenuStrip1
			// 
			this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.floatToolStripMenuItem,
            this.vector3ToolStripMenuItem,
            this.vector4ToolStripMenuItem,
            this.textureToolStripMenuItem,
            this.normalMapToolStripMenuItem});
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			this.contextMenuStrip1.Size = new System.Drawing.Size(176, 152);
			// 
			// floatToolStripMenuItem
			// 
			this.floatToolStripMenuItem.Name = "floatToolStripMenuItem";
			this.floatToolStripMenuItem.Size = new System.Drawing.Size(175, 24);
			this.floatToolStripMenuItem.Text = "Float";
			this.floatToolStripMenuItem.Click += new System.EventHandler(this.floatToolStripMenuItem_Click);
			// 
			// vector3ToolStripMenuItem
			// 
			this.vector3ToolStripMenuItem.Name = "vector3ToolStripMenuItem";
			this.vector3ToolStripMenuItem.Size = new System.Drawing.Size(175, 24);
			this.vector3ToolStripMenuItem.Text = "Vector3";
			this.vector3ToolStripMenuItem.Click += new System.EventHandler(this.vector3ToolStripMenuItem_Click);
			// 
			// vector4ToolStripMenuItem
			// 
			this.vector4ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addToolStripMenuItem,
            this.multiplyScalarToolStripMenuItem});
			this.vector4ToolStripMenuItem.Name = "vector4ToolStripMenuItem";
			this.vector4ToolStripMenuItem.Size = new System.Drawing.Size(175, 24);
			this.vector4ToolStripMenuItem.Text = "Vector4";
			this.vector4ToolStripMenuItem.Click += new System.EventHandler(this.vector4ToolStripMenuItem_Click);
			// 
			// addToolStripMenuItem
			// 
			this.addToolStripMenuItem.Name = "addToolStripMenuItem";
			this.addToolStripMenuItem.Size = new System.Drawing.Size(176, 24);
			this.addToolStripMenuItem.Text = "Add";
			this.addToolStripMenuItem.Click += new System.EventHandler(this.addToolStripMenuItem_Click);
			// 
			// multiplyScalarToolStripMenuItem
			// 
			this.multiplyScalarToolStripMenuItem.Name = "multiplyScalarToolStripMenuItem";
			this.multiplyScalarToolStripMenuItem.Size = new System.Drawing.Size(176, 24);
			this.multiplyScalarToolStripMenuItem.Text = "Multiply Scalar";
			this.multiplyScalarToolStripMenuItem.Click += new System.EventHandler(this.multiplyScalarToolStripMenuItem_Click);
			// 
			// textureToolStripMenuItem
			// 
			this.textureToolStripMenuItem.Name = "textureToolStripMenuItem";
			this.textureToolStripMenuItem.Size = new System.Drawing.Size(175, 24);
			this.textureToolStripMenuItem.Text = "Texture";
			this.textureToolStripMenuItem.Click += new System.EventHandler(this.textureToolStripMenuItem_Click);
			// 
			// normalMapToolStripMenuItem
			// 
			this.normalMapToolStripMenuItem.Name = "normalMapToolStripMenuItem";
			this.normalMapToolStripMenuItem.Size = new System.Drawing.Size(175, 24);
			this.normalMapToolStripMenuItem.Text = "Normal Map";
			this.normalMapToolStripMenuItem.Click += new System.EventHandler(this.normalMapToolStripMenuItem_Click);
			// 
			// propertyGrid1
			// 
			this.propertyGrid1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.propertyGrid1.Location = new System.Drawing.Point(924, 0);
			this.propertyGrid1.Name = "propertyGrid1";
			this.propertyGrid1.Size = new System.Drawing.Size(276, 781);
			this.propertyGrid1.TabIndex = 5;
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.processToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(1200, 28);
			this.menuStrip1.TabIndex = 6;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// processToolStripMenuItem
			// 
			this.processToolStripMenuItem.Name = "processToolStripMenuItem";
			this.processToolStripMenuItem.Size = new System.Drawing.Size(70, 24);
			this.processToolStripMenuItem.Text = "Process";
			this.processToolStripMenuItem.Click += new System.EventHandler(this.processToolStripMenuItem_Click);
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1200, 781);
			this.Controls.Add(this.menuStrip1);
			this.Controls.Add(this.propertyGrid1);
			this.Controls.Add(this.nodeGraphPanel1);
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "MainForm";
			this.Text = "Material Editor";
			this.contextMenuStrip1.ResumeLayout(false);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private NodeGraphControl.NodeGraphPanel nodeGraphPanel1;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
		private System.Windows.Forms.ToolStripMenuItem floatToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem vector3ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem vector4ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem textureToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem normalMapToolStripMenuItem;
		private System.Windows.Forms.PropertyGrid propertyGrid1;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem processToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem addToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem multiplyScalarToolStripMenuItem;
	}
}

