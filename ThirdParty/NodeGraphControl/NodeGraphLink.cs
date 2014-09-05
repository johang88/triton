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
using NodeGraphControl.Xml;

namespace NodeGraphControl
{
    /// <summary>
    /// Represents a link between two NodeGraphConnectors
    /// </summary>
    public class NodeGraphLink
    {
        /// <summary>
        /// The first end of the link, that's connected to an Output Connector
        /// </summary>
        public NodeGraphConnector Input
        {
            get { return this.m_InputConnector; }
        }
        /// <summary>
        /// The last end of the link, that's connected to an Input Connector
        /// </summary>
        public NodeGraphConnector Output
        {
            get { return this.m_OutputConnector; }
        }
        private NodeGraphConnector m_InputConnector;
        private NodeGraphConnector m_OutputConnector;
        private NodeGraphDataType m_NodeGraphDataType;

        public NodeGraphDataType NodeGraphDataType
        {
            get { return m_NodeGraphDataType; }
        }
        /// <summary>
        /// Creates a new NodeGraphLink, given input and output Connectors
        /// </summary>
        /// <param name="p_Input"></param>
        /// <param name="p_Output"></param>
        public NodeGraphLink(NodeGraphConnector p_Input, NodeGraphConnector p_Output, NodeGraphDataType p_DataType)
        {
            this.m_InputConnector = p_Input;
            this.m_OutputConnector = p_Output;
            this.m_NodeGraphDataType = p_DataType;

        }

        /// <summary>
        /// SERIALIZATION: Creates a NodeGraphLink, given an XML Serialized copy of the link and a view
        /// </summary>
        /// <param name="p_TreeNode"></param>
        /// <param name="p_View"></param>
        public NodeGraphLink(XmlTreeNode p_TreeNode, NodeGraphView p_View)
        {
            int v_InputNodeId = int.Parse(p_TreeNode.m_attributes["InputNodeId"]);
            int v_OutputNodeId = int.Parse(p_TreeNode.m_attributes["OutputNodeId"]);
            int v_InputNodeConnectorIdx = int.Parse(p_TreeNode.m_attributes["InputNodeConnectorIdx"]);
            int v_OutputNodeConnectorIdx = int.Parse(p_TreeNode.m_attributes["OutputNodeConnectorIdx"]);

            this.m_InputConnector = p_View.NodeCollection[v_InputNodeId].Connectors[v_InputNodeConnectorIdx];
            this.m_OutputConnector = p_View.NodeCollection[v_OutputNodeId].Connectors[v_OutputNodeConnectorIdx];
            this.m_NodeGraphDataType = p_View.NodeCollection[v_InputNodeId].Connectors[v_InputNodeConnectorIdx].DataType;

        }

        /// <summary>
        /// SERIALIZATION: Creates a NodeGraphLink from XML, used for inherited classes
        /// </summary>
        /// <param name="p_ObjectXml"></param>
        /// <param name="p_View"></param>
        /// <returns></returns>
        public static NodeGraphLink DeserializeFromXML(XmlTreeNode p_ObjectXml, NodeGraphView p_View)
        {
            string className = p_ObjectXml.m_nodeName;

            object[] arguments = { p_ObjectXml, p_View };

            System.Reflection.Assembly v_Assembly = System.Reflection.Assembly.GetExecutingAssembly();

            object v_Out = v_Assembly.CreateInstance(className, false,
                                                    System.Reflection.BindingFlags.CreateInstance,
                                                    null,
                                                    arguments, System.Globalization.CultureInfo.GetCultureInfo("en-us"),
                                                    null);

            return v_Out as NodeGraphLink;
        }

        /// <summary>
        /// SERIALIZATION: Creates a XML Serialized copy of the link
        /// </summary>
        /// <param name="p_XmlParentTreeNode"></param>
        /// <returns></returns>
        public XmlTreeNode SerializeToXML(XmlTreeNode p_XmlParentTreeNode)
        {
            XmlTreeNode v_Out = new XmlTreeNode(SerializationUtils.GetFullTypeName(this),p_XmlParentTreeNode);

            NodeGraphView v_View = Input.Parent.ParentView;
            NodeGraphNode v_InputNode = Input.Parent;
            NodeGraphNode v_OutputNode = Output.Parent;

            v_Out.AddParameter("InputNodeId", v_View.GetNodeIndex(v_InputNode).ToString());
            v_Out.AddParameter("OutputNodeId", v_View.GetNodeIndex(v_OutputNode).ToString());
            v_Out.AddParameter("InputNodeConnectorIdx",v_InputNode.GetConnectorIndex(Input).ToString());
            v_Out.AddParameter("OutputNodeConnectorIdx", v_OutputNode.GetConnectorIndex(Output).ToString());

            return v_Out;

        }
    }
}
