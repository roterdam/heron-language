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
    public class ModuleDefn : ClassDefn
    {
        List<ClassDefn> classes = new List<ClassDefn>();
        List<InterfaceDefn> interfaces = new List<InterfaceDefn>();
        List<EnumDefn> enums = new List<EnumDefn>();
        Dictionary<string, HeronType> types = new Dictionary<string, HeronType>();
        Dictionary<string, string> importedAliases = new Dictionary<string, string>();
        List<string> importedModules = new List<string>();
        HeronProgram program;

        public ModuleDefn(HeronProgram prog, string name)
            : base(null, name)
        {
            program = prog;
            this.name = name;
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.ModuleType;
        }

        #region heron visible functions
        [HeronVisible]
        public HeronProgram GetProgram()
        {
            return program;
        }

        [HeronVisible]
        public ModuleDefn GetGlobal()
        {
            return program.GetGlobal();
        }

        [HeronVisible]
        public ClassDefn FindClass(string s)
        {
            foreach (ClassDefn c in classes)
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
        public InterfaceDefn FindInterface(string s)
        {
            foreach (InterfaceDefn i in interfaces)
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
        EnumDefn FindEnum(string s)
        {
            foreach (EnumDefn e in enums)
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
        public void AddClass(ClassDefn x)
        {
            types.Add(x.name, x);
            classes.Add(x);
        }

        [HeronVisible]
        public void AddInterface(InterfaceDefn x)
        {
            types.Add(x.name, x);
            interfaces.Add(x);
        }

        [HeronVisible]
        public void AddEnum(EnumDefn x)
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
        public IEnumerable<ClassDefn> GetClasses()
        {
            return classes;
        }

        [HeronVisible]
        public IEnumerable<InterfaceDefn> GetInterfaces()
        {
            return interfaces;
        }

        [HeronVisible]
        public IEnumerable<EnumDefn> GetEnums()
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

        [HeronVisible]
        public IEnumerable<ModuleDefn> GetImportedModuleDefns()
        {
            foreach (string s in importedModules)
                yield return program.GetModule(s);
        }
        #endregion

        public override HeronValue Instantiate(VM vm, HeronValue[] args, ModuleInstance m)
        {
            return new ModuleInstance(this, null);
        }

        public IEnumerable<string> GetImportedModuleAliases()
        {
            return importedAliases.Keys;
        }

        public IEnumerable<string> GetImportedModuleNames()
        {
            return importedModules;
        }

        public void AddImport(string sModName, string sModAlias)
        {
            importedAliases.Add(sModAlias, sModName);
            if (!importedAliases.ContainsValue(sModName))
                importedModules.Add(sModName);
        }
    }
}
