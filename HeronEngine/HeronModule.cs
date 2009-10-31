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
    public class HeronModule 
    {
        public string name;

        List<HeronClass> classes = new List<HeronClass>();
        List<HeronInterface> interfaces = new List<HeronInterface>();
        List<HeronEnum> enums = new List<HeronEnum>();
        Dictionary<string, HeronType> types = new Dictionary<string, HeronType>();
        HeronProgram program;

        public HeronModule(HeronProgram prog, string name)
        {
            program = prog;
            this.name = name;
            program.AddModule(this);
        }

        public HeronProgram GetProgram()
        {
            return program;
        }

        internal HeronModule GetGlobal()
        {
            return program.GetGlobal();
        }

        public HeronClass GetMainClass()
        {
            return FindClass("Main");
        }

        public HeronClass GetPremainClass()
        {
            return FindClass("Premain");
        }

        internal HeronClass FindClass(string s)
        {
            foreach (HeronClass c in classes)
                if (c.name == s)
                    return c;
            return null;
        }

        public bool ContainsClass(string s)
        {
            return FindClass(s) != null;
        }

        public HeronInterface FindInterface(string s)
        {
            foreach (HeronInterface i in interfaces)
                if (i.name == s)
                    return i;
            return null;
        }

        public bool ContainsInterface(string s)
        {
            return FindInterface(s) != null;
        }

        HeronEnum FindEnum(string s)
        {
            foreach (HeronEnum e in enums)
                if (e.name == s)
                    return e;
            return null;
        }

        public bool ContainsEnum(string s)
        {
            return FindEnum(s) != null;
        }

        public void AddClass(HeronClass x)
        {
            types.Add(x.name, x);
            classes.Add(x);
        }

        public void AddInterface(HeronInterface x)
        {
            types.Add(x.name, x);
            interfaces.Add(x);
        }

        public void AddEnum(HeronEnum x)
        {
            types.Add(x.name, x);
            x.SetModule(this);
            enums.Add(x);
        }

        public IEnumerable<HeronType> GetTypes()
        {
            return types.Values;
        }

        public IEnumerable<HeronClass> GetClasses()
        {
            return classes;
        }

        public IEnumerable<HeronInterface> GetInterfaces()
        {
            return interfaces;
        }

        public IEnumerable<HeronEnum> GetEnums()
        {
            return enums;
        }

        internal HeronType FindType(string s)
        {
            if (types.ContainsKey(s))
                return types[s];
            return null;
        }

        internal void AddDotNetType(string s, Type t)
        {
            if (FindType(s) != null)
                throw new Exception("Type '" + s + "' already exists");
            types.Add(s, new DotNetClass(this, s, t));
        }

        internal void AddPrimitive(string s, PrimitiveType t)
        {
            if (FindType(s) != null)
                throw new Exception("Type '" + s + "' already exists");
            types.Add(s, t);
        }
    }
}
