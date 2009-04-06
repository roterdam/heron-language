/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

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
    public class ParsingException : Exception
    {
        public int index;
        public int row;
        public int col;
        public string line;
        public string ptr;

        public ParsingException(string s, int index, int row, int col, string line)
            : base(s + " at " + line)
        {
            this.index = index;
            this.row = row;
            this.col = col;
            this.line = line;
            this.ptr = new String(' ', col) + "^";
        }
    }
    
    /// <summary>
    /// Store everything related to the state of the parser, including the input string,
    /// current index, concrete syntax tree, and current tree node being build.
    /// </summary>
    public class ParserState
    {
        int mIndex;
        int mExtent;
        string mData;
        AstNode mTree;
        AstNode mCur;

        public ParserState(string s)
        {
            mIndex = 0;
            mExtent = 0;
            mData = s;
            mTree = new AstNode("ast", 0, mData, null);
            mCur = mTree;
        }

        public void GetLineCol(out int line, out int col)
        {
            line = 0;
            int nLastLineChar = 0;
            for (int i = 0; i < mIndex; ++i)
            {
                if (mData[i].Equals('\n'))
                {
                    line++;
                    nLastLineChar = i;
                }
            }
            col = mIndex - nLastLineChar;
        }

        public int GetDataLength()
        {
            return mData.Length;
        }

        public string GetLine(int nLine)
        {
            int n = 0;
            int cnt = 0;
            while (n < GetDataLength() && cnt < nLine)
            {
                if (mData[n] == '\n')
                    ++nLine;
                ++n;
            }
            if (n >= GetDataLength())
                return "";
            int len = 0;
            while (n + len < GetDataLength() && mData[n + len] != '\n') {
                ++len;
            }
            return mData.Substring(n, Math.Min(len, 128));
        }

        public void ThrowError(string s, int index)
        {
            int line; 
            int col; 
            GetLineCol(out line, out col);
            throw new ParsingException(s, index, line, col, GetLine(line));
        }

        public bool AtEnd()
        {
            return mIndex >= mData.Length;
        }

        public int GetIndex()
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
                return mData.Substring(mIndex, 20);
            }
        }

        public string ParserPosition
        {
            get
            {
                int line;
                int col;
                GetLineCol(out line, out col);
                string ret = "line " + line + ", column " + col + "\n";
                ret += GetLine(line);
                ret += new String(' ', col);
                ret += "^";
                return ret;
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
            return mData[mIndex];
        }

        public AstNode GetCurrentNode()
        {
            return mCur;
        }

        public AstNode CreateNode(string sLabel)
        {
            Trace.Assert(mCur != null);
            mCur = mCur.Add(sLabel, this);
            Trace.Assert(mCur != null);
            return mCur;
        }

        public void AbandonNode()
        {
            Trace.Assert(mCur != null);
            AstNode tmp = mCur;
            mCur = mCur.GetParent();
            Trace.Assert(mCur != null);
            mCur.Remove(tmp);
        }

        public void CompleteNode()
        {
            Trace.Assert(mCur != null);
            mCur.Complete(this);
            mCur = mCur.GetParent();
            Trace.Assert(mCur != null);
        }

        public AstNode GetAst()
        {
            return mTree;
        }

        public AstNode Parse(Peg.Grammar.Rule g)
        {
            if (!g.Match(this))
                return null;
                            
            if (mCur != mTree)
                throw new Exception("internal error: parse tree and parse node do not match after parsing");
            
            mCur.Complete(this);
            return mTree;
        }

        public static AstNode Parse(Peg.Grammar.Rule g, string s)
        {
            ParserState p = new ParserState(s);
            return p.Parse(g);
        }
    }
}
