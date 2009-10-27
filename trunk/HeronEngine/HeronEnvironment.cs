using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Security.Policy;
using System.Diagnostics;

namespace HeronEngine
{
    /// <summary>
    /// This is the lexical environment of a running program. 
    /// It is organized as a stack of frames. It has a temporary "result" variable
    /// and a list of arguments.
    /// </summary>
    public class Environment 
    {
        #region fields
        /// <summary>
        /// The current result value
        /// </summary>
        private HeronValue result;

        /// <summary>
        /// A list of call stack frames (also called activation records)
        /// </summary>
        private Stack<Frame> frames = new Stack<Frame>();

        /// <summary>
        /// Currently executing program. It contains global names.
        /// </summary>
        private HeronProgram program;
        #endregion

        public Environment(HeronProgram program)
        {
            Clear();
            this.program = program;
        }

        public void Clear()
        {
            frames.Clear();
            result = null;
            Initialize();
        }

        public void Initialize()
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
                throw new Exception("error occured: " + s);
        }

        /// <summary>
        /// Called when a new function execution starts.
        /// </summary>
        /// <param name="f"></param>
        /// <param name="self"></param>
        public void PushNewFrame(FunctionDefinition f, ClassInstance self)
        {
            frames.Push(new Frame(f, self));
        }

        /// <summary>
        /// Creates a new lexical scope. Roughly corresponds to an open brace ('{') in many languages.
        /// </summary>
        public void PushScope()
        {
            PushScope(new Scope());
        }

        /// <summary>
        /// Creates a new scope, with a predefined set of variable names. Useful for function arguments
        /// or class fields.
        /// </summary>
        /// <param name="scope"></param>
        public void PushScope(Scope scope)
        {
            frames.Peek().AddScope(scope);
        }

        /// <summary>
        /// Creates a new variable name in the current scope.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="o"></param>
        public void AddVar(string s, HeronValue o)
        {
            Trace.Assert(o != null);
            if (frames.Peek().HasVar(s))
                throw new Exception(s + " is already declared in the scope");
            frames.Peek().AddVar(s, o);
        }

        /// <summary>
        /// Assigns a value a variable name in the current environment.
        /// The name must already exist
        /// </summary>
        /// <param name="s"></param>
        /// <param name="o"></param>
        public void SetVar(string s, HeronValue o)
        {
            Trace.Assert(o != null);
            foreach (Frame f in frames)
                if (f.SetVar(s, o))
                    return;
            throw new Exception("Could not find variable " + s);
        }

        public void SetField(string s, HeronValue o)
        {
            Trace.Assert(o != null);
            if (frames.Count == 0)
                throw new Exception("No stack frames");
            Frame f = frames.Peek();
            if (f.self == null)
                throw new Exception("Not called from within a class");
            f.self.SetField(s, o);
        }

        /// <summary>
        /// Inidcates the current activation record is finished.
        /// </summary>
        public void PopFrame()
        {
            // Reset the returning flag, to indicate that the returning operation is completed. 
            frames.Pop();
        }

        /// <summary>
        /// Removes the current scope. Correspond roughly to a closing brace ('}').
        /// </summary>
        public void PopScope()
        {
            frames.Peek().PopScope();
        }
        /// <summary>
        /// Looks up the value or type associated with the name.
        /// Looks in each scope in the currenst stack frame until a match is found.
        /// If no match is found then the various module scopes are searched.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public HeronValue LookupName(string s)
        {
            if (frames.Count != 0)
            {
                bool bFound = false;
                HeronValue r = frames.Peek().LookupName(s, out bFound);
                if (bFound)
                    return r;
            }
            HeronModule module = GetCurrentModule();
            if (module != null)
            {
                foreach (HeronType t in module.GetTypes())
                    if (t.name == s)
                        return t;
            }
            if (program != null)
            {
                foreach (HeronType t in program.GetGlobal().GetTypes())
                    if (t.name == s)
                        return t;
            }
            

            throw new Exception("Could not find '" + s + "' in the environment");
       }

        /// <summary>
        /// Looks up a name in the local variables in current scope only.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public HeronValue LookupVar(string name)
        {
            if (frames.Count == 0)
                return null;
            return frames.Peek().LookupVar(name);
        }

        /// <summary>
        /// Returns true if the name is that of a variable in the local scope
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool HasVar(string name)
        {
            if (frames.Count == 0)
                return false;
            return frames.Peek().HasVar(name);
        }

        /// <summary>
        /// Looks up a name as a field in the current object.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public HeronValue LookupField(string name)
        {
            if (frames.Count == 0)
                return null;
            return frames.Peek().LookupField(name);
        }

        /// <summary>
        /// Returns true if the name is a field in the current object scope.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool HasField(string name)
        {
            if (frames.Count == 0)
                return false;
            return frames.Peek().HasField(name);
        }

        /// <summary>
        /// Gets the value set by the previous "return".
        /// Resets the result as well. So any subsequent calls will
        /// return null, until return is called on it.
        /// </summary>
        /// <returns></returns>
        public HeronValue GetLastResult()
        {
            HeronValue r = result;
            result = null;
            return r;
        }

        /// <summary>
        /// Gets the current activation record.
        /// </summary>
        /// <returns></returns>
        public Frame GetCurrentFrame()
        {
            return frames.Peek();
        }

        /// <summary>
        /// Returns a textual representation of the environment. 
        /// Used primarily for debugging
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (Frame f in frames)
                sb.Append(f.ToString());
            return sb.ToString();
        }

        /// <summary>
        /// Set the most recent result 
        /// </summary>
        /// <param name="ret"></param>
        public void SetResult(HeronValue ret)
        {
            result = ret;
        }

        /// <summary>
        /// Returns the module associated with the current frame.
        /// </summary>
        /// <returns></returns>
        public HeronModule GetCurrentModule()
        {
            Frame f = GetCurrentFrame();
            Trace.Assert(f != null);
            HeronModule m = f.module;
            return m;
        }

        /// <summary>
        /// Returns all frames, useful for creating a call stack 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Frame> GetFrames()
        {
            return frames;
        }
    }
}
