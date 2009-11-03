/// Heron language interpreter for Windows in C#
/// http://www.heron-language.com
/// Copyright (c) 2009 Christopher Diggins
/// Licenced under the MIT License 1.0 
/// http://www.opensource.org/licenses/mit-license.php


using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Util;

namespace Peg
{
    /// <summary>
    /// Abstract Syntax Tree node.  
    /// </summary>
    public class AstNode
    {
        int mnBegin;
        int mnCount;
        string msLabel;
        String msText;
        AstNode mpParent;
        List<AstNode> mChildren = new List<AstNode>();

        public AstNode(string label, int n, String text, AstNode p)
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

        public AstNode Add(string sLabel, ParserState p)
        {
            AstNode ret = new AstNode(sLabel, p.GetPos(), msText, this);
            mChildren.Add(ret);
            return ret;
        }

        public void Complete(ParserState p)
        {
            mnCount = p.GetPos() - mnBegin;
        }

        public AstNode GetParent()
        {
            return mpParent;
        }

        public void Remove(AstNode x)
        {
            mChildren.Remove(x);
        }

        public string GetXmlDoc()
        {
            string s = "<?xml version='1.0'?>";
            s += GetXmlText();
            return s;
        }

        public override string ToString()
        {
            return msText.SafeSubstring(mnBegin, mnCount);
        }       

        public string GetXmlText()
        {
            string s = "<" + msLabel + ">\n";

            if (GetNumChildren() == 0)
            {
                s += ToString();
            }
            else
            {
                foreach (AstNode node in mChildren)
                {
                    s += node.GetXmlText();
                }
            }
            s += "</" + msLabel + ">\n";
            return s;
        }

        public string Label
        {
            get
            {
                return msLabel;
            }
        }

        public List<AstNode> Children
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

        public AstNode GetChild(int n)
        {
            return mChildren[n];
        }

        public AstNode GetChild(string s)
        {
            foreach (AstNode node in Children)
                if (node.Label.Equals(s))
                    return node;
            return null;
        }
    }
}
