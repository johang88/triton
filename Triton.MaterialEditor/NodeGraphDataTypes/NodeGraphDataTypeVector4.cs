using NodeGraphControl;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.MaterialEditor.NodeGraphDataTypes
{
	public class NodeGraphDataTypeVector4 : NodeGraphDataType
	{
		public NodeGraphDataTypeVector4()
		{
			this.m_LinkPen = new Pen(Color.FromArgb(172, 255, 54));
			this.m_LinkArrowBrush = new SolidBrush(Color.FromArgb(172, 255, 54));
			this.m_ConnectorOutlinePen = new Pen(Color.FromArgb(172, 255, 54));
			this.m_ConnectorFillBrush = new SolidBrush(Color.FromArgb(146, 248, 0));
			this.m_TypeName = "Vector4";
		}

		public override string ToString()
		{
			return this.m_TypeName;
		}
	}
}
