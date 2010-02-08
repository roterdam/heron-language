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
        List<string> names = new List<string>();
        List<HeronValue> values = new List<HeronValue>();

        public int Count
        {
            get { return values.Count; }
        }

        public void Add(string name, HeronValue value)
        {
            lookup.Add(name, Count);
            names.Add(name);
            values.Add(value);
        }

        public HeronValue GetValue(int n)
        {
            return values[n];
        }

        public string GetName(int n)
        {
            return names[n];
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
                values[Lookup(s)] = value;
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
                values[n] = value;
            }
        }
    }
}
