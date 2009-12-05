using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    public class HeronVMDebugger
    {
        Environment env;
        List<Statement> statements = new List<Statement>();

        public HeronVMDebugger(Environment env)
        {
            this.env = env;
        }

        public void AddStatement(Statement st)
        {
            statements.Add(st);
        }

        public Peg.AstNode CurrentNode
        {
            get
            {
                if (statements.Count == 0)
                    return null;
                return statements[statements.Count - 1].node;
            }
        }

        public string CurrentLine
        {
            get
            {
                if (CurrentNode == null)
                    return "";
                return CurrentNode.CurrentLine;
            }
        }

        public int CurrentLineIndex
        {
            get
            {
                if (CurrentNode == null)
                    return -1;
                return CurrentNode.CurrentLineIndex;
            }
        }
    }
}
