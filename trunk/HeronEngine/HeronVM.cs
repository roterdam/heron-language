/// Heron language interpreter for Windows in C#
/// http://www.heron-language.com
/// Copyright (c) 2009 Christopher Diggins
/// Licenced under the MIT License 1.0 
/// http://www.opensource.org/licenses/mit-license.php

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
    public class HeronVM
    {
        #region helper classes
        /// <summary>
        /// Used for creation and deletion of scopes.
        /// Do not instantiate directly, only HeronVM creates this.
        /// <seealso cref="HeronVm.CreateFrame"/>
        /// </summary>
        public class DisposableScope : IDisposable
        {
            HeronVM vm;
            public DisposableScope(HeronVM vm)
            {
                this.vm = vm;
                vm.PushScope();
            }
            public DisposableScope(HeronVM vm, Scope scope)
            {
                this.vm = vm;
                vm.PushScope(scope);
            }
            public void Dispose()
            {
                vm.PopScope();
            }
        }

        /// <summary>
        /// Helper class for the creation and deletion of frames.
        /// Do not instantiate directly, only HeronVM creates this.
        /// <seealso cref="HeronVm.CreateFrame"/>
        /// </summary>
        public class DisposableFrame : IDisposable
        {
            HeronVM vm;
            public DisposableFrame(HeronVM vm, FunctionDefinition def, ClassInstance ci)
            {
                this.vm = vm;
                vm.PushNewFrame(def, ci);
            }
            public void Dispose()
            {
                vm.PopFrame();
            }
        }
        #endregion helper classes

        /// <summary>
        /// Lexical environment: names and keys
        /// </summary>
        Environment env;

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
        public HeronVM()
        {
            program = new HeronProgram("_untitled_");
            env = new Environment(program);

            // Load the global types
            foreach (HeronType t in program.GetGlobal().GetTypes())
                AddVar(t.name, t);
        }

        /// <summary>
        /// Clears all scopes, frames, and symbols in the environment
        /// </summary>
        public void InitializeEnvironment()
        {
            env.Clear();
        }

        #region evaluation functions
        public HeronValue EvalString(string s)
        {
            Expression x = HeronTypedAST.ParseExpr(s);
            return Eval(x); ;
        }

        public void EvalModule(string sModule)
        {
            HeronModule m = HeronTypedAST.ParseModule(program, sModule);
            RunModule(m);
        }

        public void RunPreMain(HeronModule m)
        {
            HeronClass premain = m.GetPremainClass();
            if (premain != null)
            {
                // Start the parparse 
                HeronValue o = DotNetObject.Marshal(m.GetProgram());
                premain.Instantiate(this, new HeronValue[] { o });
            }
        }

        public void RunMain(HeronModule m)
        {
            HeronClass main = m.GetMainClass();
            if (main != null)
            {
                main.Instantiate(this);
            }
        }

        public void RunModule(HeronModule m)
        {
            InitializeEnvironment();
            RunPreMain(m);
            RunMain(m);            
        }

        /// <summary>
        /// Evaluates a list expression as an IHeronEnumerator
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public IHeronEnumerator EvalList(Expression list)
        {
            HeronValue tmp = Eval(list);
            if (!(tmp is SeqValue))
                throw new Exception("Expected an enumerable value");
            return (tmp as SeqValue).GetEnumerator(this);
        }

        /// <summary>
        /// Evaluates a list expression, converting it into an IEnumerable&lt;HeronValue&gt;
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public IEnumerable<HeronValue> EvalListAsDotNet(Expression list)
        {
            return new HeronToEnumeratorAdapter(this, EvalList(list));
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
        #endregion
        
        #region scope and frame management
        /// <summary>
        /// Get the currently executing frame
        /// </summary>
        /// <returns></returns>
        public Frame GetCurrentFrame()
        {
            return env.GetCurrentFrame();
        }

        /// <summary>
        /// Used by DisposableScope to create a lexical scope in which names can live
        /// </summary>
        private void PushScope()
        {
            env.PushScope();
        }

        /// <summary>
        /// Adds a name-value group.
        /// </summary>
        private void PushScope(Scope scope)
        {
            env.PushScope(scope);
        }

        /// <summary>
        /// Removes the top-most lexical scope.
        /// </summary>
        private void PopScope()
        {
            env.PopScope();
        }

        /// <summary>
        /// Creates a scope, and when DisposableScope.Dispose is called removes it
        /// Normally you would use this as follows:
        /// <code>
        ///     using (vm.CreateScope())
        ///     {
        ///       ...
        ///     }
        /// </code>
        /// </summary>
        /// <returns></returns>
        public DisposableScope CreateScope()
        {
            return new DisposableScope(this);
        }

        /// <summary>
        /// Creates a scope, and when DisposableScope.Dispose is called removes it
        /// Normally you would use this as follows:
        /// <code>
        ///     using (vm.CreateScope(scope))
        ///     {
        ///       ...
        ///     }
        /// </code>
        /// </summary>
        /// <returns></returns>
        public DisposableScope CreateScope(Scope scope)
        {
            return new DisposableScope(this, scope);
        }

        /// <summary>
        /// Used by DisposableFrame to creates a new stack frame (activation record), containing a functions arguments 
        /// </summary>
        /// <param name="fun"></param>
        /// <param name="classInstance"></param>
        private void PushNewFrame(FunctionDefinition fun, ClassInstance classInstance)
        {
            env.PushNewFrame(fun, classInstance);
        }

        /// <summary>
        /// Used by DisposableFrame to Removes the current stack frame (activation record)
        /// </summary>
        private void PopFrame()
        {
            env.PopFrame();
            bReturning = false;
        }

        /// <summary>
        /// Creates a new frame, and returns a frame manager, which will release the frame
        /// on Dispose.
        /// </summary>
        /// <param name="fun"></param>
        /// <param name="classInstance"></param>
        /// <returns></returns>
        public DisposableFrame CreateFrame(FunctionDefinition fun, ClassInstance classInstance)
        {
            return new DisposableFrame(this, fun, classInstance);
        }
        #endregion

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

        #region variables, fields, and name management
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
        /// Checks for existence of variable in environment 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool HasVar(string name)
        {
            return env.HasVar(name);
        }

        /// <summary>
        /// Checks for existence of field in the current class instance.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool HasField(string name)
        {
            return env.HasField(name);
        }

        /// <summary>
        /// Assigns a value to a field.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        public void SetField(string name, HeronValue val)
        {
            env.SetField(name, val);
        }

        /// <summary>
        /// Looks up the value associated with a given name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public HeronValue LookupName(string name)
        {
            return env.LookupName(name);
        }

        /// <summary>
        /// Add a variable.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="initVal"></param>
        public void AddVar(string name, HeronValue initVal)
        {
            env.AddVar(name, initVal);
        }
        
        /// <summary>
        /// Add a group of variables at once
        /// </summary>
        /// <param name="vars"></param>
        public void AddVars(Scope vars)
        {
            foreach (string name in vars.Keys)
                AddVar(name, vars[name]);
        }        
        #endregion 

        /// <summary>
        /// Gets the value set by the last executed return statement
        /// </summary>
        /// <returns></returns>
        public HeronValue GetLastResult()
        {
            return env.GetLastResult();
        }

        /// <summary>
        /// Should only ever be called by the debugger.
        /// </summary>
        /// <returns></returns>
        internal Environment GetEnvironment()
        {
            return env;
        }
    }
}
