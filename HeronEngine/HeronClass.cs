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
    /// An instance of a class type.
    /// </summary>
    public class ClassDefn : HeronUserType
    {
        #region exposedFields
        List<FunctionDefn> methods = new List<FunctionDefn>();
        List<FieldDefn> fields = new List<FieldDefn>();
        HeronType baseclass = null;
        List<HeronType> interfaces = new List<HeronType>();
        FunDefnListValue ctors;
        FunctionDefn autoCtor;
        #endregion

        #region internal function

        public ClassDefn(ModuleDefn m, string name)
            : base(m, typeof(ClassDefn), name)
        {
            autoCtor = new FunctionDefn(this, "_Constructor_");
        }

        public void ResolveTypes(ModuleDefn global, ModuleDefn m)
        {
            if (baseclass != null)
                baseclass = baseclass.Resolve(global, m);
            for (int i = 0; i < interfaces.Count; ++i)
            {
                HeronType t = interfaces[i].Resolve(global, m);
                if (t == null)
                    throw new Exception(interfaces[i].name + " could not be resolved");
                InterfaceDefn hi = t as InterfaceDefn;
                if (hi == null)
                    throw new Exception(t.name + " is not an interface");
                interfaces[i] = hi;
            }
            foreach (FieldDefn f in GetFields())
                f.ResolveTypes(global, m);
            foreach (FunctionDefn f in GetAllMethods())
                f.ResolveTypes(global, m);
        }

        public bool VerifyImplements(InterfaceDefn i)
        {
            foreach (FunctionDefn f in i.GetAllMethods())
                if (!HasFunction(f))
                    return false;
            return true;
        }

        public void VerifyInterfaces()
        {
            foreach (InterfaceDefn i in interfaces)
                if (!VerifyImplements(i))
                    throw new Exception("Class '" + name + "' does not implement the interface '" + i.name + "'");
        }

        public bool HasFunction(FunctionDefn f)
        {
            foreach (FunctionDefn g in GetMethods(f.name))
                if (g.Matches(f))
                    return true;
            return false;
        }

        public void SetBaseClass(HeronType c)
        {
            baseclass = c;

            // If we know we have a base class, we add a call to the base auto-constructor
            // in our own auto-constructor
            Expression func = new ChooseField(new Name("base"), "_Constructor_");
            FunCall fc = new FunCall(func, new ExpressionList());
            autoCtor.body.statements.Add(new ExpressionStatement(fc));
        }

        [HeronVisible]
        public IEnumerable<ClassDefn> GetInheritedClasses()
        {
            if (baseclass != null)
                yield return baseclass as ClassDefn;
        }

        [HeronVisible]
        public IEnumerable<HeronType> GetImplementedInterfaces()
        {
            return interfaces;
        }

        [HeronVisible]
        public ClassDefn GetBaseClass()
        {
            return baseclass as ClassDefn;
        }

        [HeronVisible]
        public void AddImplementedInterface(HeronType i)
        {
            if (i == null)
                throw new Exception("Cannot add 'null' as an interface");
            interfaces.Add(i);
        }

        public void AddFields(ClassInstance i, ModuleInstance m)
        {
            i.AddField(new VarDesc("this"), i);

            foreach (FieldDefn field in fields)
                i.AddField(field);

            if (GetBaseClass() != null)
            {
                ClassInstance b = new ClassInstance(GetBaseClass(), m);
                GetBaseClass().AddFields(b, m);
                i.AddField(new VarDesc("base"), b);
            }
        }

        public bool HasMethod(string name)
        {
            foreach (FunctionDefn f in GetMethods(name))
                return true;
            return false;
        }

        public bool ImplementsMethod(FunctionDefn f)
        {
            List<FunctionDefn> matches = new List<FunctionDefn>(GetMethods(f.name));
            foreach (FunctionDefn fd in matches)
                if (fd.Matches(f))
                    return true;
            return false;
        }

        [HeronVisible]
        public bool Implements(InterfaceDefn i)
        {
            foreach (FunctionDefn fd in i.GetAllMethods())
                if (!ImplementsMethod(fd))
                    return false;
            
            return true;
        }

        [HeronVisible]
        public bool InheritsFrom(ClassDefn c)
        {
            string s = c.name;
            if (s == name)
                return true;
            if (GetBaseClass() != null)
                return GetBaseClass().InheritsFrom(c);
            return false;
        }

        #endregion

        #region public methods

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="env"></param>
        /// <returns></returns>
        public override HeronValue Instantiate(VM vm, HeronValue[] args, ModuleInstance m)
        {
            ClassInstance r = new ClassInstance(this, m);
            AddFields(r, m);
            CallConstructor(vm, args, m, r);
            return r;
        }

        protected void CallConstructor(VM vm, HeronValue[] args, ModuleInstance mi, ClassInstance ci)
        {
            // First we are going to invoke the auto-constructor
            GetAutoContructor().Invoke(ci, vm, new HeronValue[] { });

            List<FunctionDefn> ctorlist = new List<FunctionDefn>(GetMethods("Constructor"));

            if (ctorlist == null)
                return;
            ctors = new FunDefnListValue(ci, "Constructor", ctorlist);
            if (ctors.Count == 0)
            {
                if (args.Length > 0)
                    throw new Exception("No constructors have been defined and default constructor accepts no arguments");
                else
                    return;
            }

            FunctionValue o = ctors.Resolve(vm, args);
            if (o == null)
                throw new Exception("No matching constructor could be found");

            o.Apply(vm, args);
        }

        [HeronVisible]
        public IEnumerable<FieldDefn> GetFields()
        {
            return fields;
        }

        [HeronVisible]
        public void AddMethod(FunctionDefn x)
        {
            methods.Add(x);
        }

        [HeronVisible]
        public void AddField(FieldDefn x)
        {
            fields.Add(x);
        }

        [HeronVisible]
        public override FieldDefn GetField(string s)
        {
            foreach (FieldDefn f in fields)
                if (f.name == s)
                    return f;
            return base.GetField(s);
        }

        [HeronVisible]
        public FunDefnListValue GetCtors()
        {
            return ctors;
        }

        public override IEnumerable<FunctionDefn> GetAllMethods()
        {
            yield return autoCtor;
            foreach (FunctionDefn f in GetDeclaredMethods())
                yield return f;
            if (baseclass != null)
                foreach (FunctionDefn f in (baseclass as ClassDefn).GetAllMethods())
                    yield return f;
        }


        [HeronVisible]
        public IEnumerable<FunctionDefn> GetDeclaredMethods()
        {
            return methods;
        }

        [HeronVisible]
        public IEnumerable<FunctionDefn> GetInheritedMethods()
        {
            if (baseclass != null)
                foreach (FunctionDefn f in (baseclass as ClassDefn).GetAllMethods())
                    yield return f;
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.ClassType;
        }

        public override HeronValue GetFieldOrMethod(string name)
        {
            List<FunctionDefn> fs = new List<FunctionDefn>(GetMethods(name));

            // Look for static exposedFunctions to return
            if (fs.Count != 0)
                return new FunDefnListValue(Null, name, fs);
            return base.GetFieldOrMethod(name);
        }

        public bool HasBaseClass()
        {
            return baseclass != null;
        }

        public string GetInheritedClassName()
        {
            return baseclass.name;
        }

        public override int GetHierarchyDepth()
        {
            if (HasBaseClass())
                return 1 + GetBaseClass().GetHierarchyDepth();
            else
                return 1;
        }

        public FunctionDefn GetAutoContructor()
        {
            return autoCtor;
        }
        #endregion
    }
}
