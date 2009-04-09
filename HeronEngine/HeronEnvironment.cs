using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    /// <summary>
    /// This is an association list of objects with names.
    /// This is used as a mechanism for creating scoped names.
    /// </summary>
    public class ObjectTable : Dictionary<String, HeronObject>
    {
        public void Add(VarObject o)
        {
            Add(o.name, o);
        }
    }

    /// <summary>
    /// A stack frame, also called an activation record, contains information 
    /// about the calling function. It also has a stack of object-name association lists
    /// which correspond to scopes. A stack is used so that names declared in one scope override
    /// any similiarly named variables in previous scopes.
    /// </summary>
    public class Frame : Stack<ObjectTable>
    {
        public Frame(Function f, Instance self)
        {
            this.function = f;
            this.self = self;
        }

        public HeronObject LookupName(string s)
        {
            foreach (ObjectTable tbl in this)
                if (tbl.ContainsKey(s))
                    return tbl[s];

            if (self.HasField(s))
                return self.GetFieldValue(s);

            if (self.HasMethod(s))
                return self.GetMethod(s);

            return null;
        }

        /// <summary>
        /// Function associated with this activation record 
        /// </summary>
        public Function function;

        /// <summary>
        /// The 'this' pointer if applicable 
        /// </summary>
        public Instance self;       
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
        private HeronObject result;

        /// <summary>
        /// A flag that is set to true when a return statement occurs. 
        /// </summary>
        bool bReturning = false;

        /// <summary>
        /// A list of call stack frames 
        /// </summary>
        Stack<Frame> frames = new Stack<Frame>();

        /// <summary>
        /// This is for containing names at the module level.
        /// Currently only the top of the stack is used as a single shared global module scope 
        /// I plan eventually on adding import statements.
        /// </summary>
        Stack<ObjectTable> moduleScopes = new Stack<ObjectTable>();
        #endregion

        public Environment()
        {
            PushNewFrame(null, null);
            PushScope();
            PushModuleScope();
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
                throw new Exception("error occured: " + s);
        }

        /// <summary>
        /// Called when a new function execution starts.
        /// </summary>
        /// <param name="f"></param>
        /// <param name="self"></param>
        public void PushNewFrame(Function f, Instance self)
        {
            Assure(!bReturning, "cannot push a new frame while returning from another");
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
        /// Creates a new module namespace. 
        /// </summary>
        public void PushModuleScope()
        {
            moduleScopes.Push(new ObjectTable());
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
        /// Looks up the value or type associated with the name.
        /// Looks in each scope in the currenst stack frame until a match is found.
        /// If no match is found then the various module scopes are searched.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public HeronObject LookupName(string s)
        {
            if (frames.Count != 0)
            {
                HeronObject r = frames.Peek().LookupName(s);
                if (r != null)
                    return r;
            }
            foreach (ObjectTable scope in moduleScopes)
            {
                if (scope.ContainsKey(s))
                    return scope[s];
            }
            return null;
        }

        /// <summary>
        /// Gets the value set by the previous "return".
        /// Resets the result as well. So any subsequent calls will
        /// return null, until return is called on it.
        /// </summary>
        /// <returns></returns>
        public HeronObject GetLastResult()
        {
            HeronObject r = result;
            result = null;
            return result;
        }

        /// <summary>
        /// Adds a new name to the top-most module namespace
        /// </summary>
        public void AddModuleVar(string s, HeronObject o)
        {
            if (moduleScopes.Peek().ContainsKey(s))
                throw new Exception(s + " is already declared in the top-level module scope");
            moduleScopes.Peek().Add(s, o);
        }

        /// <summary>
        /// Gets the current activation record.
        /// </summary>
        /// <returns></returns>
        public Frame GetCurrentFrame()
        {
            return frames.Peek();
        }
    }
}
