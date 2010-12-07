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
    /// Used to identify where an error occured in an input string. 
    /// Such as the line number and character number.
    /// </summary>
    public class ParseLocation
    {
        public ParseLocation(string input, int begin, int end)
        {
            this.input = input;
            this.begin = begin;
            this.end = end;
        }

        public string input;
        public int begin;
        public int end;
    }

    /// <summary>
    /// Contains detailed information about the locaion
    /// of a parse error, for constructing useful strings.
    /// </summary>
    public class ParseExceptionContext
    {
        public ParseLocation location;
        public string msg;
        public int row;
        public int col;
        public string line;
        public string ptr;
        public Rule rule;

        public ParseExceptionContext(string s, int begin, int end, Rule r, string msg)
        {
            location = new ParseLocation(s, begin, end);
            this.msg = msg;
            s.GetRowCol(begin, out row, out col);
            line = s.GetLine(begin);
            StringBuilder sb = new StringBuilder();
            int i = begin - 1;
            ptr = "^";
            while (i >= 0)
            {
                if (s[i] == '\n')
                    break;
                if (s[i] == '\t')
                    ptr = "\t" + ptr;
                else
                    ptr = " " + ptr;
                --i;
            }
            sb.Append('^');
        }
    }
    
    /// <summary>
    /// An exception that provides information about where a parsing error 
    /// occrured.
    /// </summary>
    public class ParsingException : Exception
    {
        public ParseExceptionContext context;

        public ParsingException(string s, int begin, int end, Rule r, string msg)
            : base(msg)
        {          
            AddContext(s, begin, end, r, msg);
        }

        public void AddContext(string s, int begin, int end, Rule r, string msg)
        {
            context = new ParseExceptionContext(s, begin, end, r, msg);
        }
    }

    public interface IParserState
    {
        int GetPos();
        void SetPos(int n);
        void GotoNext();
        char GetChar();
        string GetInput();
        bool AtEnd();
        void CreateNode(string label);
        void CompleteNode();
        void AbandonNode();
    }
    
    /// <summary>
    /// Store everything related to the state of the parser, including the input string,
    /// current index, concrete syntax tree, and current tree node being build.
    /// </summary>
    public class ParserState : IParserState
    {
        int mIndex;
        int mExtent = 0;
        string mInput;
        ParseNode mTree;
        ParseNode mCur;

        public ParserState(string s)
            : this(s, 0)
        {
        }

        public ParserState(string s, int from)
        {
            mInput = s;
            mIndex = from;
            mTree = new ParseNode("ast", mIndex, mInput, null);
            mCur = mTree;
        }

        public int GetInputLength()
        {
            return mInput.Length;
        }

        public string GetInput()
        {
            return mInput;
        }

        public bool AtEnd()
        {
            return mIndex >= mInput.Length;
        }

        public int GetPos()
        {
            return mIndex; 
        }

        public int GetExtent()
        {
            return mExtent;
        }

        public string CurrentLine
        {
            get
            {
                return mInput.SafeSubstring(mIndex, 20);
            }
        }

        public string PrefixContext
        {
            get
            {
                return mInput.SafeSubstring(mIndex - 20, 20);
            }
        }

        public string SuffixContext
        {
            get
            {
                return mInput.SafeSubstring(mIndex, 20);
            }
        }

        public void SetPos(int pos)
        {
            mIndex = pos;
            if (mIndex > mExtent) mExtent = mIndex;
        }

        public void IncIndex()
        {
            if (++mIndex > mExtent)
                mExtent = mIndex;
        }

        public void GotoNext()
        {
            if (AtEnd())
            {
                throw new Exception("passed the end of input");
            }
            IncIndex();
        }

        public char GetChar()
        {
            if (AtEnd()) 
            { 
                throw new Exception("passed end of input"); 
            }
            return mInput[mIndex];
        }

        public ParseNode GetCurrentNode()
        {
            return mCur;
        }

        public void CreateNode(string sLabel)
        {
            Debug.Assert(mCur != null);
            mCur = mCur.Add(sLabel, this);
            Debug.Assert(mCur != null);
        }

        public void AbandonNode()
        {
            Debug.Assert(mCur != null);
            ParseNode tmp = mCur;
            mCur = mCur.GetParent();
            Debug.Assert(mCur != null);
            mCur.Remove(tmp);
        }

        public void CompleteNode()
        {
            Debug.Assert(mCur != null);
            mCur.Complete(this);
            mCur = mCur.GetParent();
            Debug.Assert(mCur != null);
        }

        public ParseNode GetAst()
        {
            return mTree;
        }

        public ParseNode Parse(Rule r)
        {
            Debug.Assert(r != null);

            if (!r.Match(this))
                return null;
                            
            if (mCur != mTree)
                throw new Exception("internal error: parse tree and parse node do not match after parsing");
            
            mCur.Complete(this);
            return mTree;
        }
        
        public static ParseNode Parse(Rule r, string s)
        {
            Debug.Assert(r != null);

            ParserState p = new ParserState(s);
            ParseNode node = p.Parse(r);
            if (node == null)
                return null;
            if (node.Label != "ast")
                throw new Exception("no root AST node");
            if (node.GetNumChildren() != 1)
                throw new Exception("more than one child node parsed");
            return node.GetChild(0);
        }
    }

    /// <summary>
    /// Parse 
    /// </summary>
    public static class Parser
    {
        static public bool Parse(Rule r, string s, int from, out string output, out ParseNode node)
        {
            ParserState state = new ParserState(s, from);
            node = state.Parse(r);
            int n = state.GetPos() - from;
            if (node != null && n > 0)
                output = s.Substring(from, n);
            else
                output = "";
            return node != null;
        }

        static public bool Parse(Rule r, string s, out ParseNode node)
        {
            ParserState state = new ParserState(s);
            node = state.Parse(r);
            return node != null;
        }

        static public bool Parse(Rule r, string s, out string output, out ParseNode node)
        {
            return Parse(r, s, 0, out output, out node);
        }

        static public bool Parse(Rule r, string s, out string output)
        {
            ParseNode node;
            return Parse(r, s, 0, out output, out node);
        }

        static public bool Parse(Rule r, string s, int from, out string output)
        {
            ParseNode node;
            return Parse(r, s, from, out output, out node);
        }

        static public bool Parse(Rule r, string s)
        {
            ParserState state = new ParserState(s);
            return state.Parse(r) != null;
        }
    }
}
