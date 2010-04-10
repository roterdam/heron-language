/// Heron language interpreter for Windows in C#
/// http://www.heron-language.com
/// Copyright (c) 2009 Christopher Diggins
/// Licenced under the MIT License 1.0 
/// http://www.opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace HeronEngine
{
    /// <summary>
    /// This is an association list of objects with names.
    /// This is used as a mechanism for creating scoped names.
    /// </summary>
    public class Scope 
    {
        Dictionary<string, int> lookup = new Dictionary<string, int>();
        List<VarDesc> vars = new List<VarDesc>();
        List<HeronValue> values = new List<HeronValue>();

        public int Count
        {
            get { return values.Count; }
        }

        public void Add(VarDesc v)
        {
            int n = Count;
            vars.Add(v);
            values.Add(HeronValue.Null);
            lookup.Add(v.name, n);
        }

        public void Add(VarDesc v, HeronValue x)
        {
            x = v.Coerce(x);
            int n = Count;
            vars.Add(v);
            values.Add(x);
            lookup.Add(v.name, n);
        }

        public HeronValue GetValue(int n)
        {
            return values[n];
        }

        public HeronValue GetType(int n)
        {
            return GetVar(n).type;
        }

        public string GetName(int n)
        {
            return GetVar(n).name;
        }

        public VarDesc GetVar(int n)
        {
            return vars[n];
        }

        public bool HasName(string s)
        {
            return Lookup(s) >= 0;
        }

        public int Lookup(string s)
        {
            if (!lookup.ContainsKey(s))
                return -1;
            else
                return lookup[s];
        }

        public HeronValue this[string s]
        {
            get
            {
                return values[Lookup(s)];
            }
            set
            {
                int n = Lookup(s);
                HeronValue tmp = vars[n].Coerce(value);
                values[n] = tmp;
            }
        }

        public HeronValue this[int n]
        {
            get
            {
                return values[n];
            }
            set
            {
                HeronValue tmp = vars[n].Coerce(value);
                values[n] = tmp;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Count; ++i)
            {
                sb.AppendLine(GetName(i) + " = " + GetValue(i).ToString());
            }
            return sb.ToString();
        }
    }
}
