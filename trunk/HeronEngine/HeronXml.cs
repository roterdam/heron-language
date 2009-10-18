using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using Peg;

namespace HeronEngine
{
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
