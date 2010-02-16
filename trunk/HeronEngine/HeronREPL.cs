using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace HeronEngine
{
    public class REPL 
    {
        VM vm = new VM();
        bool bExit = false;
        string sPrompt = ">>";

        public REPL()
        {
            vm.AddVar("vm", vm);
            RegisterFunction("Exit");
            Print("Welcome to the Heron interactive interpreter");
            Print("To exit, type in 'Exit()' at the prompt");
            Print("");
        }

        public void RegisterFunction(string s)
        {
            MethodInfo mi = GetType().GetMethod(s);
            DotNetMethod m = new DotNetMethod(mi, DotNetObject.Marshal(this));
            vm.AddVar(s, m);
        }

        public void Run()
        {
            while (!bExit)
                Print(Eval(Read()));
        }

        public void Exit()
        {
            bExit = true;
        }

        public string Read()
        {
            Console.Write(sPrompt);
            return Console.ReadLine();
        }

        public static string Eval(string s)
        {
            s = s.TrimEnd();
            throw new NotImplementedException();
        }

        public static void Print(string s)
        {
            Console.WriteLine(s);
        }
    }
}
