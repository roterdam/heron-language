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
        Stack<ObjectTable> scopes = new Stack<ObjectTable>();

        public Frame(HeronFunction f, ClassInstance self)
        {
            this.function = f;
            this.self = self;
            if (function != null)
                type = function.GetParentType();
            if (type != null)
                module = type.GetModule();
        }

        public void AddScope(ObjectTable scope)
        {
            scopes.Push(scope);
        }

        public void PopScope()
        {
            scopes.Pop();
        }

        public HeronObject LookupName(string s, out bool bFound)
        {
            bFound = true;

            // NOTE: it would be more efficient to make "this" 
            // a special operator. 
            if (s == "this")
                return self;

            foreach (ObjectTable tbl in scopes)
                if (tbl.ContainsKey(s))
                    return tbl[s];

            if (self != null)
            {
                if (self.HasField(s))
                    return self.GetFieldOrMethod(s);

                if (self.HasMethod(s))
                    return self.GetMethods(s);
            }

            if (module != null)
            {
                HeronType t = module.FindType(s);
                if (t != null)
                    return t;
                t = module.GetGlobal().FindType(s);
                if (t != null)
                    return t;
            }

            bFound = false;
            return null;
        }

        public HeronObject LookupVar(string s)
        {
            if (s == "this")
                return self;
            foreach (ObjectTable tbl in scopes)
                if (tbl.ContainsKey(s))
                    return tbl[s];
            return null;
        }

        public HeronObject LookupField(string s)
        {
            if (self == null)
                return null;

            if (self.HasField(s))
                return self.GetFieldOrMethod(s);

            return null;
        }

        public bool HasVar(string s)
        {
            if (s == "this")
                return true;
            foreach (ObjectTable tbl in scopes)
                if (tbl.ContainsKey(s))
                    return true;
            return false;
        }

        public bool SetVar(string s, HeronObject o)
        {
            foreach (ObjectTable tbl in scopes)
            {
                if (tbl.ContainsKey(s))
                {
                    tbl[s] = o;
                    return true;
                }
            }
            return false;
        }

        public void AddVar(string s, HeronObject o)
        {
            scopes.Peek().Add(s, o);
        }

        public bool HasField(string s)
        {
            if (self == null)
                return false;
            return self.HasField(s);
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
            if (self != null && self.hclass != null)
                sb.Append(self.hclass.name);
            else
                sb.Append("null");
            sb.AppendLine("]");

            foreach (ObjectTable tab in scopes)
            {
                sb.Append("[scope]");
                sb.Append(tab.ToString());
            }
            return sb.ToString();
        }

        /// <summary>
        /// Function associated with this activation record 
        /// </summary>
        public HeronFunction function = null;

        /// <summary>
        /// The 'this' pointer if applicable 
        /// </summary>
        public ClassInstance self = null;

        /// <summary>
        /// The type which contains the function
        /// </summary>
        public HeronType type = null;

        /// <summary>
        /// The module containing the type
        /// </summary>
        public HeronModule module = null;
    }
}
