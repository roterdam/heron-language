using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Peg;
using HeronTests;

namespace HeronEngine
{
    /// <summary>
    /// Encapsulates the following components:
    /// - concrete syntax tree parser (Parser)
    /// - abstract syntax tree parser (HeronParser)
    /// - lexical environment (Environment)
    /// - evaluator (Expression and Statement)
    /// </summary>
    public class HeronExecutor
    {
        Environment env = new Environment();
        HeronModule module; 

        public HeronExecutor()
        {
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
            HeronModule m = HeronParser.ParseModule(sModule);
            EvalModule(m);
        }

        public void InitializeEnvironment()
        {
            env.Clear();
            foreach (HeronClass c in module.classes)
                env.AddModuleVar(c.name, c);
        }

        public void EvalModule(HeronModule m)
        {
            module = m;
            InitializeEnvironment();
            HeronClass main = m.GetMainClass();
            if (main == null)
                throw new Exception("Could not evaluate module " 
                    + m.name + " without a class named Main");

            HeronObject inst = main.Instantiate(env, new HeronObject[] { });            
        }

        public void PrecompileModule(HeronModule m)
        {
            module = m;
            InitializeEnvironment();
            HeronClass premain = m.GetPremainClass();
            if (premain == null)
                return;

            HeronObject inst = premain.Instantiate(env, new HeronObject[] { });
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
