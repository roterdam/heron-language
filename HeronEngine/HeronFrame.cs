/// Heron language interpreter for Windows in C#
/// http://www.heron-language.com
/// Copyright (c) 2009 Christopher Diggins
/// Licenced under the MIT License 1.0 
/// http://www.opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    /// <summary>
    /// A stack frame, also called an activation record, contains information 
    /// about the calling function. It also has a stack of object-name association lists
    /// which correspond to scopes. A stack is used so that names declared in one scope override
    /// any similiarly named variables in previous scopes.
    /// </summary>
    public class Frame 
    {
        /// <summary>
        /// Function associated with this activation record 
        /// </summary>
        public FunctionDefn function = null;

        /// <summary>
        /// The 'this' pointer if applicable 
        /// </summary>
        public ClassInstance self = null;

        /// <summary>
        /// The type which contains the function
        /// </summary>
        public HeronType type = null;

        /// <summary>
        /// The moduleDef containing the type
        /// </summary>
        public ModuleDefn moduleDef = null;

        /// <summary>
        /// The module instance containing the "self" type.
        /// </summary>
        public ModuleInstance moduleInstance = null;

        /// <summary>
        /// A list of scopes, which are effectivelyh name value pairs
        /// </summary>
        private Stack<Scope> scopes = new Stack<Scope>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="f"></param>
        /// <param name="self"></param>
        public Frame(FunctionDefn f, ClassInstance self)
        {
            this.function = f;
            this.self = self;
            this.moduleInstance = self == null ? null : self.GetModuleInstance();
            if (function != null)
                type = function.GetContainingType();
            if (type != null)
                moduleDef = type.GetModule();
            AddScope(new Scope());
        }

        public void AddScope(Scope scope)
        {
            scopes.Push(scope);
        }

        public void PopScope()
        {
            scopes.Pop();
        }

        public HeronValue LookupName(string s)
        {
            // Look in the scopes starting with the innermost 
            // and moving to the outermost.
            // The outermost scope contains the arguments
            foreach (Scope tbl in scopes)
                if (tbl.ContainsKey(s))
                    return tbl[s];

            // Nothing found in the local vars, 
            // So we look in the "this" pointer (called "self")
            // Note that "self" may be a class instance, or a moduleDef
            // instance
            if (self != null)
            {
                HeronValue r = self.GetFieldOrMethod(s);
                if (r != null)
                    return r;
            
                // Nothing found in the "this" pointer. So 
                // we look if it has an enclosing module instance pointer.
                // And use that 
                ModuleInstance mi = self.GetModuleInstance();
                if (mi != null)
                {
                    r = mi.GetFieldOrMethod(s);
                    if (r != null)
                        return r;
                }
            }

            // Look to see if the name is a type in the current module definition.
            if (moduleDef != null)
            {
                HeronType t = moduleDef.FindType(s);
                if (t != null)
                    return t;

                // Look to see if the name is a type in one of the imported module definitions.
                List<HeronType> candidates = new List<HeronType>();
                foreach (ModuleDefn defn in moduleDef.GetImportedModuleDefns())
                {
                    t = defn.FindType(s);
                    if (t != null)
                        candidates.Add(t);
                }

                if (candidates.Count > 1)
                    throw new Exception("Ambiguous name resolution. Multiple modules contain a type named " + s);
                if (candidates.Count == 1)
                    return candidates[1];
            }

            return null;
        }

        public HeronValue GetVar(string s)
        {
            foreach (Scope tbl in scopes)
                if (tbl.ContainsKey(s))
                    return tbl[s];
            throw new Exception("No field named '" + s + "' could be found");
        }

        public HeronValue GetField(string s)
        {
            if (self != null && self.HasField(s))
                return self.GetField(s);
            else if (moduleInstance != null && moduleInstance.HasField(s))
                return moduleInstance.GetField(s);
            else
                throw new Exception("No field named '" + s + "' could be found");
        }

        public void SetField(string s, HeronValue v)
        {
            if (self != null && self.HasField(s))
                self.SetField(s, v);
            else if (moduleInstance != null && moduleInstance.HasField(s))
                moduleInstance.SetField(s, v);
            else
                throw new Exception("No field named '" + s + "' could be found");
        }

        public bool HasVar(string s)
        {
            foreach (Scope tbl in scopes)
                if (tbl.ContainsKey(s))
                    return true;
            return false;
        }

        public bool SetVar(string s, HeronValue o)
        {
            foreach (Scope tbl in scopes)
            {
                if (tbl.ContainsKey(s))
                {
                    tbl[s] = o;
                    return true;
                }
            }
            return false;
        }

        public void AddVar(string s, HeronValue o)
        {
            if (scopes.Peek().ContainsKey(s))
                throw new Exception(s + " is already declared in the scope");
            scopes.Peek().Add(s, o);
        }

        public bool HasField(string s)
        {
            if (self != null && self.HasField(s))
                return true;
            if (moduleInstance != null && moduleInstance.HasField(s))
                return true;
            return false;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[frame, function = ");

            if (function != null)
                sb.Append(function.name); 
            else
                sb.Append("null");
            sb.Append(", class = ");
            if (self != null)
                sb.Append(self.GetClassName());
            else
                sb.Append("null");
            sb.AppendLine("]");

            foreach (Scope tab in scopes)
            {
                sb.Append("[scope]");
                sb.Append(tab.ToString());
            }
            return sb.ToString();
        }

        public string SimpleDescription
        {
            get
            {
                string r = "";
                if (moduleDef == null)
                    r += "unknown_module : ";
                else
                    r += moduleDef.name + " : ";

                if (self == null)
                    r += "unknown_class : ";
                else
                    r += self.GetClassName() + " : ";
                if (function == null)
                    r += "unknown_function";
                else
                    r += function.ToString();
                return r;
            }
        }

        public IEnumerable<Scope> GetScopes()
        {
            return scopes;
        }
    }
}
