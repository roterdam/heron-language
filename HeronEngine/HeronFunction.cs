using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace HeronEngine
{
    /// <summary>
    /// Represents the definition of a Heron function or member function
    /// </summary>
    public class Function : HeronObject
    {
        public string name;
        public HeronClass hclass;
        public Statement body;
        public FormalArgs formals;
        public string rettype;

        public void Call(Environment env, Instance self, HeronObject[] args)
        {
            if (self == null)
            {
                CallAsMethod(env, self, args);
            }
            else
            {
                CallAsFunction(env, args);
            }
        }

        private void PushArgsAsScope(Environment env, HeronObject[] args)
        {
            int n = formals.Count;
            Trace.Assert(n == args.Length);
            for (int i = 0; i < n; ++i)
                env.AddVar(formals[i].name, args[i]);
        }

        private void CallAsMethod(Environment env, Instance self, HeronObject[] args)
        {
            // Create a stack frame 
            env.PushNewFrame(this, self);

            // Create a scope containing the member fields of the class
            self.PushFieldsAsScope(env);

            // Create a new scope containing the arguments 
            PushArgsAsScope(env, args);

            // Execute the function body
            body.Execute(env);

            // Pop the arguments scope
            env.PopScope();

            // Pop the member fields scope
            env.PopScope(); 

            // Pop the calling frame
            env.PopFrame(); 
        }

        private void CallAsFunction(Environment env, HeronObject[] args)
        {
            // Create a stack frame 
            env.PushNewFrame(this, null);

            // Create a new scope containing the arguments 
            PushArgsAsScope(env, args);

            // Execute the function body
            body.Execute(env);

            // Pop the arguments scope
            env.PopScope();

            // Pop the calling frame
            env.PopFrame();
        }

        private void VerifyArgTypes(HeronObject[] args)
        {
            // TODO:
        }

        private void VerifySelfType(HeronObject self)
        {
            // TODO:
        }

        private void VerifyReturnType(Environment env)
        {
            // TODO:
        }
    }

    /// <summary>
    /// Function names can be overloaded, so when looking up a function by name,
    /// a set of functions is returned.
    /// </summary>
    public class FunctionTable : Dictionary<string, List<Function>>
    {
        public void Add(Function f)
        {
            string s = f.name;
            if (!ContainsKey(s))
                Add(s, new List<Function>());
            this[s].Add(f);
        }
    }
}
