using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    /// <summary>
    /// This is an association list of objects with names.
    /// This is mostly used as a mechanism for creating scoped names.
    /// </summary>
    public class ObjectTable : Dictionary<String, HeronObject>
    {
    }

    /// <summary>
    /// A stack frame, also called an activation record, contains information 
    /// about the calling function. It also has a stack of object-name association lists
    /// which correspond to scopes. A stack is used so that names declared in one scope override
    /// any similiarly named variables in previous scopes.
    /// </summary>
    public class Frame : Stack<ObjectTable>
    {
        public Frame(Function f, HeronObject self)
        {
            this.function = f;
            this.self = self;
        }

        /// <summary>
        /// Function associated with this activation record 
        /// </summary>
        public Function function;

        /// <summary>
        /// The 'this' pointer if applicable 
        /// </summary>
        public HeronObject self;
       
    }

    /// <summary>
    /// This is the lexical environment of a running program. 
    /// It is organized as a stack of frames. It has a temporary "result" variable
    /// and a list of arguments.
    /// </summary>
    public class Environment : HeronObject
    {
        #region fields
        /// <summary>
        /// The current result value
        /// </summary>
        public HeronObject result;

        /// <summary>
        /// A flag that is set to true when a return statement occurs. 
        /// </summary>
        bool bReturning = false;

        /// <summary>
        /// A list of call stack frames 
        /// </summary>
        Stack<Frame> frames = new Stack<Frame>();
        #endregion

        public Environment()
        {
            PushNewFrame(null, null);
            PushScope();
        }

        /// <summary>
        /// Throw an exception if condition is not true. However, not an assertion. 
        /// This is used to check for exceptional run-time condition.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="s"></param>
        private void Assure(bool b, string s)
        {
            if (!b)
            {
                throw new Exception("error occured: " + s);
            }
        }

        /// <summary>
        /// Called when a new function execution starts.
        /// </summary>
        /// <param name="f"></param>
        /// <param name="self"></param>
        public void PushNewFrame(Function f, HeronObject self)
        {
            Assure(!bReturning, "can not push a new frame while returning from another");
            frames.Push(new Frame(f, self));
        }

        /// <summary>
        /// Creates a new lexical scope. Roughly corresponds to an open brace ('{') in many languages.
        /// </summary>
        public void PushScope()
        {
            PushScope(new ObjectTable());
        }

        /// <summary>
        /// Creates a new scope, with a predefined set of variable names. Useful for function arguments
        /// or class fields.
        /// </summary>
        /// <param name="scope"></param>
        public void PushScope(ObjectTable scope)
        {
            frames.Peek().Push(scope);
        }

        /// <summary>
        /// Creates a new variable name in the current scope.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="o"></param>
        public void AddVar(string s, HeronObject o)
        {
            if (frames.Peek().Peek().ContainsKey(s))
                throw new Exception(s + " is already declared in the scope");
            frames.Peek().Peek().Add(s, o);
        }

        /// <summary>
        /// Assigns a value a variable name in the current environment.
        /// The name must already exist
        /// </summary>
        /// <param name="s"></param>
        /// <param name="o"></param>
        public void SetVar(string s, HeronObject o)
        {
            frames.Peek().Peek()[s] = o;
        }

        /// <summary>
        /// Inidcates the current activation record is finished.
        /// </summary>
        public void PopFrame()
        {
            // Reset the returning flag, to indicate that the returning operation is completed. 
            bReturning = false;
            frames.Pop();
        }

        /// <summary>
        /// Removes the current scope. Correspond roughly to a closing brace ('}').
        /// </summary>
        public void PopScope()
        {
            frames.Peek().Pop();
        }

        /// <summary>
        /// Returns true if a return statement was executed and we are leaving the current scope.
        /// </summary>
        /// <returns></returns>
        public bool IsReturning()
        {
            return bReturning;
        }

        /// <summary>
        /// This is used by loops over statements to check whether a return statement, a break 
        /// statement, or a throw statement was called. Currently only return statements are supported.
        /// </summary>
        /// <returns></returns>
        public bool ShouldExitScope()
        {
            return IsReturning();
        }

        /// <summary>
        /// Called by a return statement. Sets the function result, and sets a flag to indicate 
        /// to executing statement groups that execution should terminate.
        /// </summary>
        /// <param name="ret"></param>
        public void Return(HeronObject ret)
        {
            Assure(!bReturning, "internal error, returning flag was not reset");
        }

        /// <summary>
        /// Looks up the value (which is possibly also a type) associated with the name.
        /// Looks in each scope starting with the most recently created until a match is found.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public HeronObject GetVar(string s)
        {
            if (frames.Count == 0)
                return null;
            foreach (ObjectTable tbl in frames.Peek())
                if (tbl.ContainsKey(s))
                    return tbl[s];
            return null;
        }
    }
}
