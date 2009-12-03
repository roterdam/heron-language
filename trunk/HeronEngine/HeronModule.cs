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
    public class HeronModule : HeronValue
    {
        [HeronVisible]
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

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.ModuleType;
        }

        #region heron ivisble functions
        [HeronVisible]
        public HeronProgram GetProgram()
        {
            return program;
        }

        [HeronVisible]
        public HeronModule GetGlobal()
        {
            return program.GetGlobal();
        }

        [HeronVisible]
        public HeronClass GetMainClass()
        {
            return FindClass("Main");
        }

        [HeronVisible]
        public HeronClass GetMetaClass()
        {
            return FindClass("Meta");
        }

        [HeronVisible]
        public HeronClass FindClass(string s)
        {
            foreach (HeronClass c in classes)
                if (c.name == s)
                    return c;
            return null;
        }

        [HeronVisible]
        public bool ContainsClass(string s)
        {
            return FindClass(s) != null;
        }

        [HeronVisible]
        public HeronInterface FindInterface(string s)
        {
            foreach (HeronInterface i in interfaces)
                if (i.name == s)
                    return i;
            return null;
        }

        [HeronVisible]
        public bool ContainsInterface(string s)
        {
            return FindInterface(s) != null;
        }

        [HeronVisible]
        HeronEnum FindEnum(string s)
        {
            foreach (HeronEnum e in enums)
                if (e.name == s)
                    return e;
            return null;
        }

        [HeronVisible]
        public bool ContainsEnum(string s)
        {
            return FindEnum(s) != null;
        }

        [HeronVisible]
        public void AddClass(HeronClass x)
        {
            types.Add(x.name, x);
            classes.Add(x);
        }

        [HeronVisible]
        public void AddInterface(HeronInterface x)
        {
            types.Add(x.name, x);
            interfaces.Add(x);
        }

        [HeronVisible]
        public void AddEnum(HeronEnum x)
        {
            types.Add(x.name, x);
            x.SetModule(this);
            enums.Add(x);
        }

        [HeronVisible]
        public IEnumerable<HeronType> GetTypes()
        {
            return types.Values;
        }

        [HeronVisible]
        public IEnumerable<HeronClass> GetClasses()
        {
            return classes;
        }

        [HeronVisible]
        public IEnumerable<HeronInterface> GetInterfaces()
        {
            return interfaces;
        }

        [HeronVisible]
        public IEnumerable<HeronEnum> GetEnums()
        {
            return enums;
        }

        [HeronVisible]
        public HeronType FindType(string s)
        {
            if (types.ContainsKey(s))
                return types[s];
            return null;
        }

        [HeronVisible]
        public void AddDotNetType(string s, Type t)
        {
            if (FindType(s) != null)
                throw new Exception("Type '" + s + "' already exists");
            types.Add(s, new DotNetClass(this, s, t));
        }

        [HeronVisible]
        public void AddPrimitive(string s, HeronType t)
        {
            if (FindType(s) != null)
                throw new Exception("Type '" + s + "' already exists");
            types.Add(s, t);
        }
        #endregion
    }
}
