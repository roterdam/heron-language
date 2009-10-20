/// Heron language interpreter for Windows in C#
/// http://www.heron-language.com
/// Copyright (c) 2009 Christopher Diggins
/// Licenced under the MIT License 1.0 
/// http://www.opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using Peg;

namespace HeronEngine
{
    /// <summary>
    /// NOT USED YET!
    /// </summary>
    public class AstToXML
    {
        XmlDocument doc = new XmlDocument();

        public XmlDocument Xml
        {
            get { return doc; }
        }

        public AstToXML(AstNode node)
        {
            CreateElement(node, doc.DocumentElement);
        }

        public XmlElement CreateElement(AstNode node, XmlElement parent)
        {
            XmlElement r = doc.CreateElement(node.Label);
            parent.AppendChild(r);
            return r;
        }
    }
}
