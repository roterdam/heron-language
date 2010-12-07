/// Heron language interpreter for Windows in C#
/// http://www.heron-language.com
/// Copyright (c) 2009 Christopher Diggins
/// Licenced under the MIT License 1.0 
/// http://www.opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Peg
{
    /// <summary>
    /// Concrete Syntax Tree node.  
    /// </summary>
    public class ParseNode
    {
        public int mnBegin;
        public int mnCount;
        public string msLabel;
        public String msText;
        public ParseNode mpParent;
        public List<ParseNode> mChildren = new List<ParseNode>();

        public ParseNode(string label, int n, String text, ParseNode p)
        {
            msLabel = label;
            msText = text;
            mnBegin = n;
            mnCount = -1;
            mpParent = p;
        }

        public int GetIndex()
        {
            return mnBegin;
        }

        public ParseNode Add(string sLabel, ParserState p)
        {
            ParseNode ret = new ParseNode(sLabel, p.GetPos(), msText, this);
            mChildren.Add(ret);
            return ret;
        }

        public void Complete(ParserState p)
        {
            mnCount = p.GetPos() - mnBegin;
        }

        public ParseNode GetParent()
        {
            return mpParent;
        }

        public void Remove(ParseNode x)
        {
            mChildren.Remove(x);
        }

        public string GetXmlDoc()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<?xml version='1.0'?>");
            GetXmlText(sb);
            return sb.ToString();
        }

        public override string ToString()
        {
            return msText.SafeSubstring(mnBegin, mnCount);
        }

        public String GetXmlText()
        {
            StringBuilder sb = new StringBuilder();
            GetXmlText(sb);
            return sb.ToString();
        }

        public void GetXmlText(StringBuilder sb)
        {
            sb.Append("<");
            sb.Append(msLabel);
            sb.Append(">");

            if (GetNumChildren() == 0)
            {
                sb.Append(ToString());
            }
            else
            {
                foreach (ParseNode node in mChildren)
                {
                    node.GetXmlText(sb);
                }
            }
            sb.Append("</");
            sb.Append(msLabel);
            sb.AppendLine(">");
        }

        public string Label
        {
            get
            {
                return msLabel;
            }
        }

        public List<ParseNode> Children
        {
            get
            {
                return mChildren;
            }
        }

        public int GetNumChildren()
        {
            return mChildren.Count;
        }

        public ParseNode GetChild(int n)
        {
            return mChildren[n];
        }

        public ParseNode GetChild(string s)
        {
            foreach (ParseNode node in Children)
                if (node.Label.Equals(s))
                    return node;
            return null;
        }

        public IEnumerable<ParseNode> GetChildren(string s)
        {
            foreach (ParseNode node in Children)
                if (node.Label.Equals(s))
                    yield return node;
        }
        
        public bool HasChild(string s)
        {
            return GetChild(s) != null;
        }

        public int CurrentLineIndex
        {
            get
            {
                return msText.LineOfIndex(mnBegin);
            }
        }

        public string BeginningText
        {
            get
            {
                return msText.SafeSubstring(mnBegin, 20) + "...";
            }
        }
    }
}
