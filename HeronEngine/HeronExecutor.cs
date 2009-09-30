using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Peg;
using HeronTests;
using System.Diagnostics;

namespace HeronEngine
{
    /// <summary>
    /// This represents the current state of the Heron virtual machine. 
    /// </summary>
    public class HeronExecutor
    {
        /// <summary>
        /// Lexical environment: names and keys
        /// </summary>
        Environment env;

        /// <summary>
        /// Currently execution module
        /// </summary>
        HeronModule module;

        /// <summary>
        /// Currently executing program
        /// </summary>
        HeronProgram program;

        /// <summary>
        /// A flag that is set to true when a return statement occurs. 
        /// </summary>
        bool bReturning = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public HeronExecutor()
        {
            program = new HeronProgram();
            env = new Environment(program);
        }

        #region evaluation functions
        public HeronValue EvalString(string s)
        {
            Expression x = HeronParser.ParseExpr(s);
            return Eval(x); ;
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
                HeronValue o = DotNetObject.Marshal(module.GetProgram());
                premain.Instantiate(this, new HeronValue[] { o });
            }

            HeronClass main = m.GetMainClass();
            if (main != null)
                main.Instantiate(this);            
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

        public void AddVar(string name, HeronValue initVal)
        {
            env.AddVar(name, initVal);
        }
        
        /// <summary>
        /// Call this instead of "Expression.Eval()", this way you can set
        /// breakpoints etc.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public HeronValue Eval(Expression value)
        {
            return value.Eval(this);
        }

        /// <summary>
        /// Call this instead of "Stsatement.Eval()", this way you can set
        /// breakpoints etc.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public void Eval(Statement statement)
        {
            statement.Eval(this);
        }
        
        /// <summary>
        /// Creates a lexical scope in which names can live
        /// </summary>
        public void PushScope()
        {
            env.PushScope();
        }
        
        /// <summary>
        /// Removes the top-most lexical scope.
        /// </summary>
        public void PopScope()
        {
            env.PopScope();
        }

        /// <summary>
        /// Removes the current activation-frame 
        /// </summary>
        public void PopFrame()
        {
            env.PopFrame();
        }

        /// <summary>
        /// This is used by loops over statements to check whether a return statement, a break 
        /// statement, or a throw statement was called. Currently only return statements are supported.
        /// </summary>
        /// <returns></returns>
        public bool ShouldExitScope()
        {
            return bReturning;
        }
        
        /// <summary>
        /// Sets the value of a field in the environement
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        public void SetVar(string name, HeronValue val)
        {
            env.SetVar(name, val);
        }

        /// <summary>
        /// Called by a return statement. Sets the function result, and sets a flag to indicate 
        /// to executing statement groups that execution should terminate.
        /// </summary>
        /// <param name="ret"></param>
        public void Return(HeronValue ret)
        {
            Trace.Assert(!bReturning, "internal error, returning flag was not reset");
            bReturning = true;
            env.SetResult(ret);
        }

        public bool HasVar(string name)
        {
            return env.HasVar(name);
        }

        public bool HasField(string name)
        {
            return env.HasField(name);
        }

        public void SetField(string name, HeronValue val)
        {
            env.SetField(name, val);
        }

        public HeronValue LookupName(string type)
        {
            return env.LookupName(type);
        }

        public void PushNewFrame(FunctionDefinition fun, ClassInstance classInstance)
        {
            env.PushNewFrame(fun, classInstance);
        }

        public HeronValue GetLastResult()
        {
            return env.GetLastResult();
        }
    }
}
