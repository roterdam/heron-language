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
        Stack<NameValueTable> scopes = new Stack<NameValueTable>();

        public Frame(FunctionDefinition f, ClassInstance self)
        {
            this.function = f;
            this.self = self;
            if (function != null)
                type = function.GetParentType();
            if (type != null)
                module = type.GetModule();
        }

        public void AddScope(NameValueTable scope)
        {
            scopes.Push(scope);
        }

        public void PopScope()
        {
            scopes.Pop();
        }

        public HeronValue LookupName(string s, out bool bFound)
        {
            bFound = true;

            // TODO: it would be more efficient to make "this" 
            // parsed as a special expression. 
            if (s == "this")
                return self;

            // TODO: it would be more efficient to make "null" 
            // parsed as a special expression. 
            if (s == "null")
                return HeronValue.Null;

            foreach (NameValueTable tbl in scopes)
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

        public HeronValue LookupVar(string s)
        {
            if (s == "this")
                return self;
            foreach (NameValueTable tbl in scopes)
                if (tbl.ContainsKey(s))
                    return tbl[s];
            return null;
        }

        public HeronValue LookupField(string s)
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
            foreach (NameValueTable tbl in scopes)
                if (tbl.ContainsKey(s))
                    return true;
            return false;
        }

        public bool SetVar(string s, HeronValue o)
        {
            foreach (NameValueTable tbl in scopes)
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

            foreach (NameValueTable tab in scopes)
            {
                sb.Append("[scope]");
                sb.Append(tab.ToString());
            }
            return sb.ToString();
        }

        /// <summary>
        /// Function associated with this activation record 
        /// </summary>
        public FunctionDefinition function = null;

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
