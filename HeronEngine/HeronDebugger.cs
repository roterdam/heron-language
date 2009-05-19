using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    public static class HeronDebugger
    {
        class DebugData
        {
            Expression GetExpr()
            {
                if (this is DebugExpr)
                    return (this as DebugExpr).expr;
                return null;
            }

            Statement GetStatement()
            {
                if (this is DebugStatement)
                    return (this as DebugStatement).statement;
                return null;
            }
        }

        class DebugExpr : DebugData
        {
            public Expression expr;
            public DebugExpr(Expression x)
            {
                expr = x;
            }
        }

        class DebugStatement : DebugData
        {
            public Statement statement;
            public DebugStatement(Statement s)
            {
                statement = s;
            }
        }

        static Stack<DebugData> objects = new Stack<DebugData>();

        public static void TraceExpr(Expression x)
        {
            //objects.Push(new DebugExpr(x));
        }

        public static void TraceStatement(Statement s)
        {
            //objects.Push(new DebugStatement(s));
            //Console.WriteLine(s.ToString());
        }
    }
}
