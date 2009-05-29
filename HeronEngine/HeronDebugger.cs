using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    public static class HeronDebugger
    {
        static Stack<Object> objects = new Stack<Object>();

        public static void TraceExpr(Expression x)
        {
            //objects.Push(new DebugExpr(x));
        }

        public static void TraceStatement(Statement s)
        {
            //objects.Push(new DebugStatement(s));
            //Console.WriteLine(s.ToString());
        }

        public static void TraceFunction(HeronFunction f)
        {
            // TODO:
        }
    }
}
