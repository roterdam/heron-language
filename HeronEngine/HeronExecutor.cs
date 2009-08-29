using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Peg;
using HeronTests;

namespace HeronEngine
{
    /// <summary>
    /// BUG: this is possibly not reentrant. 
    /// </summary>
    public class HeronExecutor
    {
        Environment env;
        HeronModule module;
        HeronProgram program;

        public HeronExecutor()
        {
            program = new HeronProgram();
            env = new Environment(program);
        }

        #region evaluation functions
        public HeronObject EvalExpr(string s)
        {
            Expression x = HeronParser.ParseExpr(s);
            HeronObject o = x.Eval(env);
            return o;
        }

        public void EvalModule(string sModule)
        {
            HeronModule m = HeronParser.ParseModule(program, sModule);
            EvalModule(m);
        }

        public void InitializeEnvironment()
        {
            env.Clear();
        }

        public void EvalModule(HeronModule m)
        {
            module = m;
            InitializeEnvironment();
            HeronClass premain = m.GetPremainClass();
            if (premain != null)
            {
                HeronObject o = DotNetObject.Marshal(module.GetProgram());
                premain.Instantiate(env, new HeronObject[] { o });
            }

            HeronClass main = m.GetMainClass();
            if (main != null)
                main.Instantiate(env);            
        }
        #endregion

        public Environment GetEnv()
        {
            return env;
        }

        public HeronModule GetModule()
        {
            return module;
        }
    }
}
