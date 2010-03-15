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
        public class Import : HeronValue
        {
            [HeronVisible] public string module;
            [HeronVisible] public string alias;
            [HeronVisible] public ExpressionList args;

            public Import(string alias, string module, ExpressionList args)
            {
                this.module = module;
                this.alias = alias;
                this.args = args;
            }

            public override HeronType GetHeronType()
            {
                return PrimitiveTypes.ImportType;
            }
        }

        List<ClassDefn> classes = new List<ClassDefn>();
        List<InterfaceDefn> interfaces = new List<InterfaceDefn>();
        List<EnumDefn> enums = new List<EnumDefn>();
        Dictionary<string, HeronType> types = new Dictionary<string, HeronType>();
        List<Import> imports = new List<Import>();
        ProgramDefn program;
        string fileName = "no file";

        public ModuleDefn(ProgramDefn prog, string name, string sFileName)
            : base(null, name)
        {
            program = prog;
            this.name = name;
            this.module = this;
            types.Add(name, this);
            this.fileName = sFileName;
        }

        public string FileName
        {
            get
            {
                return fileName;
            }
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.ModuleType;
        }

        #region heron visible functions
        [HeronVisible]
        public ProgramDefn GetProgram()
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
            HeronType r = FindTypeLocally(s);
            if (r != null)
                return r;
            foreach (ModuleDefn def in GetImportedModuleDefns())
            {
                r = def.FindTypeLocally(s);
                if (r != null)
                    return r;
            }
            return null;
        }

        [HeronVisible]
        public HeronType FindTypeLocally(string s)
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
            types.Add(s, DotNetClass.Create(s, t));
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
            foreach (string s in GetImportedModuleNames())
                yield return program.GetModule(s);
        }
        #endregion

        public void AddFields(ModuleInstance inst, ModuleInstance parent)
        {
            inst.AddField("this", inst);

            foreach (FieldDefn field in GetFields())
                inst.AddField(field.name, HeronValue.Null);

            if (GetBaseClass() != null)
            {
                ModuleDefn baseMod = GetBaseClass() as ModuleDefn;
                if (baseMod == null)
                    throw new Exception("The base type of the module must be a module: " + GetBaseClass().name);

                ModuleInstance baseInst = new ModuleInstance(baseMod, parent);
                baseMod.AddFields(baseInst, parent);
                inst.AddField("base", baseInst);
            }
        }

        public override HeronValue Instantiate(VM vm, HeronValue[] args, ModuleInstance m)
        {
            ModuleInstance r = new ModuleInstance(this, m);
            AddFields(r, m);
            foreach (Import i in imports)
            {
                ModuleDefn importModDef = vm.LookupModuleDefn(i.module);
                HeronValue[] importArgs = vm.EvalList(i.args).ToArray();
                ModuleInstance importInstance = importModDef.Instantiate(vm, args, r) as ModuleInstance;
                if (importInstance == null)
                    throw new Exception("Failed to create loaded module instance");
                r.imports.Add(i.alias, importInstance);
            }
            CallConstructor(vm, args, m, r);
            return r;
        }

        public IEnumerable<string> GetImportedModuleAliases()
        {
            foreach (Import i in imports)
                yield return i.alias;
        }

        public IEnumerable<string> GetImportedModuleNames()
        {
            List<string> r = new List<string>();
            foreach (Import i in GetImports())
                if (!r.Contains(i.module))
                    r.Add(i.module);
            return r;
        }

        public void AddImport(string sModAlias, string sModName, ExpressionList args)
        {
            imports.Add(new Import(sModAlias, sModName, args));
        }

        public bool IsImportedModule(string sModName)
        {
            return GetImportedModuleDefn(sModName) != null;
        }

        public ModuleDefn GetImportedModuleDefn(string sModName)
        {
            foreach (ModuleDefn def in GetImportedModuleDefns())
                if (def.name == sModName)
                    return def;
            return null;
        }

        public void ResolveTypes()
        {
            if (HasBaseClass())
            {
                string s = GetInheritedClassName();
                ModuleDefn baseModule = program.GetModule(s);
                SetBaseClass(baseModule);
            }

            foreach (InterfaceDefn i in GetInterfaces())
                i.ResolveTypes(this);
            foreach (ClassDefn c in GetClasses())
                c.ResolveTypes(this);

            base.ResolveTypes(this);
        }

        [HeronVisible]
        public List<Import> GetImports()
        {
            return imports;
        }
    }
}
