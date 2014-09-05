/*

Copyright (c) 2011, Thomas ICHE
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the 
following conditions are met:

        * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
        * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer 
          in the documentation and/or other materials provided with the distribution.
        * Neither the name of PeeWeeK.NET nor the names of its contributors may be used to endorse or promote products derived from this 
          software without specific prior written permission.


THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, 
OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT 
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

*/
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Text;

namespace NodeGraphControl
{
    /// <summary>
    /// NodeGraphDataType contains information for validating links inside the graph. The class contains color information in order to 
    /// present nodes & links in a pleasant way.
    /// NodeGraphDataType is the abstract base for link typing. In order to use default type use NodeGraphDataTypeBase
    /// </summary>
    public abstract class NodeGraphDataType
    {
        public Pen          LinkPen             { get { return this.m_LinkPen; } }
        public SolidBrush   LinkArrowBrush      { get { return this.m_LinkArrowBrush; } }
        public Pen          ConnectorOutlinePen { get { return this.m_ConnectorOutlinePen; } }
        public SolidBrush   ConnectorFillBrush  { get { return this.m_ConnectorFillBrush; } }

        protected Pen           m_LinkPen;
        protected SolidBrush    m_LinkArrowBrush;
        protected Pen           m_ConnectorOutlinePen;
        protected SolidBrush    m_ConnectorFillBrush;

        protected string m_TypeName;
    }

    /// <summary>
    /// NodeGraphDataTypeBase is the generic NodeGraphDataType for link typing. It has a name of "Generic" and colored feedback as black.
    /// It serves as a base for generic node creation and as an example implementation. As it is the base this type is default registered into NodeGraphView.KnownDataTypes
    /// </summary>
    public class NodeGraphDataTypeBase : NodeGraphDataType
    {

        public NodeGraphDataTypeBase()
        {
            this.m_LinkPen = new Pen(Color.FromArgb(120, 120, 120));
            this.m_LinkArrowBrush = new SolidBrush(Color.FromArgb(120, 120, 120));
            this.m_ConnectorOutlinePen = new Pen(Color.FromArgb(60, 60, 60));
            this.m_ConnectorFillBrush = new SolidBrush(Color.FromArgb(40, 40, 40));
            this.m_TypeName = "Generic";
        }

        public override string  ToString()
        {
            return this.m_TypeName;
        }

    }

}
