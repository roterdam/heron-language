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
    /// An instance of an enum type.
    /// </summary>
    public class EnumDefn : HeronUserType
    {
        List<String> values = new List<String>();

        public EnumDefn(ModuleDefn m, string name)
            : base(m, typeof(EnumDefn), name)
        {
        }

        public override IEnumerable<FunctionDefn> GetAllMethods()
        {
            yield break;
        }

        public override HeronValue Instantiate(VM vm, HeronValue[] args, ModuleInstance m)
        {
            throw new Exception("Cannot instantiate an enumeration");
        }

        public void AddValue(string s)
        {
            values.Add(s);
        }

        public string GetValue(int n)
        {
            return values[n];
        }

        public int GetNumValues()
        {
            return values.Count;
        }

        public bool HasValue(string s)
        {
            return values.IndexOf(s) >= 0;
        }

        public IEnumerable<string> GetValues()
        {
            return values;
        }

        public override HeronValue GetFieldOrMethod(string name)
        {
            if (!HasValue(name))
                return base.GetFieldOrMethod(name);
            return new EnumInstance(this, name);
        }

        public override bool Equals(object obj)
        {
            EnumDefn e = obj as EnumDefn;
            if (e == null)
                return false;
            return e.name == name;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.EnumType;
        }
    }
}
