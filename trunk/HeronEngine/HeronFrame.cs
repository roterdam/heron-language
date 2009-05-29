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
    public class Frame : Stack<ObjectTable>
    {
        public Frame(HeronFunction f, Instance self)
        {
            this.function = f;
            this.self = self;
        }

        public HeronObject LookupName(string s, out bool bFound)
        {
            bFound = true;

            // NOTE: it would be more efficient to make "this" 
            // a special operator. 
            if (s == "this")
                return self;

            foreach (ObjectTable tbl in this)
                if (tbl.ContainsKey(s))
                    return tbl[s];

            if (self != null)
            {
                if (self.HasField(s))
                    return self.GetFieldOrMethod(s);

                if (self.HasMethod(s))
                    return self.GetMethods(s);
            }

            bFound = false;
            return null;
        }

        public HeronObject LookupVar(string s)
        {
            if (s == "this")
                return self;
            foreach (ObjectTable tbl in this)
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
            foreach (ObjectTable tbl in this)
                if (tbl.ContainsKey(s))
                    return true;
            return false;
        }

        public bool SetVar(string s, HeronObject o)
        {
            foreach (ObjectTable tbl in this)
            {
                if (tbl.ContainsKey(s))
                {
                    tbl[s] = o;
                    return true;
                }
            }
            return false;
        }

        public bool HasField(string s)
        {
            if (self == null)
                return false;
            return self.HasField(s);
        }

        /// <summary>
        /// Function associated with this activation record 
        /// </summary>
        public HeronFunction function;

        /// <summary>
        /// The 'this' pointer if applicable 
        /// </summary>
        public Instance self;
    }
}
