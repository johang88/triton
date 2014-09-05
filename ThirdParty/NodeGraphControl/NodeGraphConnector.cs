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
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace NodeGraphControl
{
    /// <summary>
    /// Represents a connector on a node
    /// </summary>
    public class NodeGraphConnector
    {
        /// <summary>
        /// The parent node that contains the connector
        /// </summary>
        public NodeGraphNode Parent { get { return this.m_oParentNode; } }
        /// <summary>
        /// Name of the connector that will be displayed
        /// </summary>
        public string Name { get { return this.m_Name; } }
        /// <summary>
        /// Type of the connector (input/output)
        /// </summary>
        public ConnectorType Type { get { return this.m_oConnectorType; } }
        /// <summary>
        /// Data type object (from NodeGraphView.KnownDataTypes)
        /// </summary>
        internal NodeGraphDataType DataType
        {
            get { return m_oDataType; }
            set { m_oDataType = value; }
        }


        private string m_Name;
        private NodeGraphNode m_oParentNode;
        private NodeGraphView m_oView;
        private ConnectorType m_oConnectorType;
        private int m_iConnectorIndex;
        private NodeGraphDataType m_oDataType;



        /// <summary>
        /// Creates a new NodeGraphConnector, given a name, a parent container, type and index
        /// </summary>
        /// <param name="p_Name">The display name of the connector</param>
        /// <param name="p_parent">Reference to the parent NodeGraphNode</param>
        /// <param name="p_ConnectorType">Type of the connector (input/output)</param>
        /// <param name="p_ConnectorIndex">Connector Index</param>
        public NodeGraphConnector(string p_Name, NodeGraphNode p_parent, ConnectorType p_ConnectorType, int p_ConnectorIndex)
        {
            this.m_Name = p_Name;
            this.m_oParentNode = p_parent;
            this.m_oView = p_parent.ParentView;
            this.m_oConnectorType = p_ConnectorType;
            this.m_iConnectorIndex = p_ConnectorIndex;
            this.m_oDataType = p_parent.ParentView.KnownDataTypes["Generic"];
        }

        /// <summary>
        /// Creates a new NodeGraphConnector, given a name, a parent container, type, index and DataType
        /// </summary>
        /// <param name="p_Name">The display name of the connector</param>
        /// <param name="p_parent">Reference to the parent NodeGraphNode</param>
        /// <param name="p_ConnectorType">Type of the connector (input/output)</param>
        /// <param name="p_ConnectorIndex">Connector Index</param>
        public NodeGraphConnector(string p_Name, NodeGraphNode p_parent, ConnectorType p_ConnectorType, int p_ConnectorIndex, string p_NodeGraphDataTypeName)
        {
            this.m_Name = p_Name;
            this.m_oParentNode = p_parent;
            this.m_oView = p_parent.ParentView;
            this.m_oConnectorType = p_ConnectorType;
            this.m_iConnectorIndex = p_ConnectorIndex;
            this.m_oDataType = p_parent.ParentView.KnownDataTypes[p_NodeGraphDataTypeName];
        }
        /// <summary>
        /// Returns the visible area of the connector
        /// </summary>
        /// <returns>a rectangle determining the visible area of the connector</returns>
        public Rectangle GetArea()
        {
            Point v_Position;
            Rectangle v_ConnectorRectangle;

            if (m_oConnectorType == ConnectorType.InputConnector)
            {

                v_Position = m_oView.ParentPanel.ViewToControl(new Point(m_oParentNode.X, (m_oParentNode.Y + m_oView.ParentPanel.NodeHeaderSize + 6 + (m_iConnectorIndex * 16))));
                v_ConnectorRectangle = new Rectangle(v_Position.X, v_Position.Y,
                                                       (int)(12 * m_oView.CurrentViewZoom),
                                                       (int)(8 * m_oView.CurrentViewZoom));

            }
            else
            {

                v_Position = m_oView.ParentPanel.ViewToControl(new Point(m_oParentNode.X + (m_oParentNode.HitRectangle.Width - 12), (m_oParentNode.Y + m_oView.ParentPanel.NodeHeaderSize + 6 + (m_iConnectorIndex * 16))));
                v_ConnectorRectangle = new Rectangle(v_Position.X, v_Position.Y,
                                                       (int)(12 * m_oView.CurrentViewZoom),
                                                       (int)(8 * m_oView.CurrentViewZoom));
            }

            return v_ConnectorRectangle;
        }

        /// <summary>
        /// Returns the Click Area of the connector
        /// </summary>
        /// <returns>a rectangle determining the Click area of the connector</returns>
        public Rectangle GetHitArea()
        {
            Point v_Position;
            Rectangle v_ConnectorRectangle;
            int Bleed = m_oView.ParentPanel.ConnectorHitZoneBleed; 
                
            if (m_oConnectorType == ConnectorType.InputConnector)
            {
                v_Position = m_oView.ParentPanel.ViewToControl(new Point(m_oParentNode.X, (m_oParentNode.Y + m_oView.ParentPanel.NodeHeaderSize + 6 + (m_iConnectorIndex * 16))));

                v_ConnectorRectangle = new Rectangle(v_Position.X - Bleed, v_Position.Y - Bleed,
                                                       (int)(12 * m_oView.CurrentViewZoom) + (2 * Bleed),
                                                       (int)(8 * m_oView.CurrentViewZoom) + (2 * Bleed));

            }
            else
            {

                v_Position = m_oView.ParentPanel.ViewToControl(new Point(m_oParentNode.X + (m_oParentNode.HitRectangle.Width - 12), (m_oParentNode.Y + m_oView.ParentPanel.NodeHeaderSize + 6 + (m_iConnectorIndex * 16))));
                v_ConnectorRectangle = new Rectangle(v_Position.X - Bleed, v_Position.Y - Bleed,
                                                       (int)(12 * m_oView.CurrentViewZoom) + (2 * Bleed),
                                                       (int)(8 * m_oView.CurrentViewZoom) + (2 * Bleed));
            }

            return v_ConnectorRectangle;
        }
        /// <summary>
        /// Returns the position of the Displayed Text
        /// </summary>
        /// <param name="e">PaintEventArgs used for measure</param>
        /// <returns>Position of the text</returns>
        public Point GetTextPosition(PaintEventArgs e)
        {
            Point v_TextPosition;

            if (m_oConnectorType == ConnectorType.InputConnector)
            {
                v_TextPosition = m_oView.ParentPanel.ViewToControl(new Point(m_oParentNode.X + 16, m_oParentNode.Y + m_oView.ParentPanel.NodeHeaderSize + 4 + (m_iConnectorIndex * 16)));
                }
            else
            {
                SizeF measure = e.Graphics.MeasureString(this.Name, m_oView.ParentPanel.NodeScaledConnectorFont);
                v_TextPosition = m_oView.ParentPanel.ViewToControl(new Point(m_oParentNode.X + (m_oParentNode.HitRectangle.Width), m_oParentNode.Y + m_oView.ParentPanel.NodeHeaderSize + 4 + (m_iConnectorIndex * 16)));
                v_TextPosition.X = v_TextPosition.X - (int)(16.0f * m_oView.CurrentViewZoom) - (int)measure.Width;
            }

            return v_TextPosition;
        }



        /// <summary>
        /// Draws the connector
        /// </summary>
        /// <param name="e"></param>
        /// <param name="ConnectorIndex"></param>
        public void Draw(PaintEventArgs e, int ConnectorIndex)
        {
            
            Rectangle v_ConnectorRectangle = this.GetArea();
            // CONNECTOR COLORING
            // TODO: ADD BOOLEAN FOR COLORING
            Pen v_ConnectorOutline;
            SolidBrush v_ConnectorFill;
            if (this.m_oView.ParentPanel.UseLinkColoring)
            {
                v_ConnectorOutline =    this.m_oDataType.ConnectorOutlinePen;
                v_ConnectorFill =       this.m_oDataType.ConnectorFillBrush;
            }
            else
            {
                v_ConnectorOutline =    this.m_oView.ParentPanel.ConnectorOutline;
                v_ConnectorFill =       this.m_oView.ParentPanel.ConnectorFill;
            }

            e.Graphics.FillRectangle(v_ConnectorFill, v_ConnectorRectangle);
            e.Graphics.DrawRectangle(v_ConnectorOutline, v_ConnectorRectangle);

            // If under zoom requirements for connector text...
            if (m_oView.CurrentViewZoom > m_oView.ParentPanel.NodeConnectorTextZoomTreshold)
            {
                Point v_TextPosition = GetTextPosition(e);

                e.Graphics.DrawString(this.Name, m_oView.ParentPanel.NodeScaledConnectorFont, m_oView.ParentPanel.ConnectorText, (float)v_TextPosition.X, (float)v_TextPosition.Y);       
            
            }

        }
        /// <summary>
        /// Virtual: Sample Process for a node, must be overriden in child class, processes only unlinked inputs.
        /// </summary>
        /// <returns>NodeGraphData depending of the validity</returns>
        public virtual NodeGraphData Process()
        {
            if (this.m_oConnectorType == ConnectorType.OutputConnector)
            {
                return m_oParentNode.Process();
            }
            else
            {
                NodeGraphConnector v_Connector = m_oView.ParentPanel.GetLink(this);
                if(v_Connector == null) return new NodeGraphInvalidData(this.m_oParentNode,"Connector:" + this.Name + " NOT LINKED");
                else return v_Connector.Process();
            }
        }

        /// <summary>
        /// Returns whether the node can be processed or not
        /// </summary>
        /// <returns></returns>
        public bool CanProcess()
        {
            if (this.m_oConnectorType == ConnectorType.OutputConnector)
            {
                return true;
            }
            else
            {
                NodeGraphConnector v_Connector = m_oView.ParentPanel.GetLink(this);
                if (v_Connector == null) return false;
                else return true;
            }
        }
    }

    /// <summary>
    /// The type of connector associated to the Node
    /// </summary>
    public enum ConnectorType
    {
        InputConnector,
        OutputConnector
    }
}
