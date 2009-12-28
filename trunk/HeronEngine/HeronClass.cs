﻿using System;
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
        #region fields
        List<FunctionDefn> methods = new List<FunctionDefn>();
        List<FieldDefn> fields = new List<FieldDefn>();
        HeronType baseclass = null;
        List<HeronType> interfaces = new List<HeronType>();
        FunDefnListValue ctors;
        #endregion

        #region internal function

        public ClassDefn(ModuleDefn m, string name)
            : base(m, typeof(ClassDefn), name)
        {
        }

        public void ResolveTypes()
        {
            if (baseclass != null)
                if (baseclass is UnresolvedType)
                    baseclass = (baseclass as UnresolvedType).Resolve();
            for (int i = 0; i < interfaces.Count; ++i)
            {
                HeronType t = interfaces[i];
                if (t is UnresolvedType)
                {
                    HeronType ht = (t as UnresolvedType).Resolve();
                    InterfaceDefn hi = ht as InterfaceDefn;
                    if (hi == null)
                        throw new Exception(ht.name + " is not an interface");
                    interfaces[i] = hi;
                }
            }
            foreach (FieldDefn f in GetFields())
                f.ResolveTypes();
            foreach (FunctionDefn f in GetAllMethods())
                f.ResolveTypes();
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
        public void AddInterface(HeronType i)
        {
            if (i == null)
                throw new Exception("Cannot add 'null' as an interface");
            interfaces.Add(i);
        }

        public void AddFields(ClassInstance i, ModuleInstance m)
        {
            i.AddField("this", i);

            foreach (FieldDefn field in fields)
                i.AddField(field.name, null);

            if (GetBaseClass() != null)
            {
                ClassInstance b = new ClassInstance(GetBaseClass(), m);
                GetBaseClass().AddFields(b, m);
                i.AddField("base", b);
            }
        }

        public bool HasMethod(string name)
        {
            foreach (FunctionDefn f in GetMethods(name))
                return true;
            return false;
        }

        [HeronVisible]
        public bool Implements(InterfaceDefn i)
        {
            string s = i.name;
            foreach (InterfaceDefn i2 in interfaces)
                if (i2.name == s)
                    return true;
            if (GetBaseClass() != null)
                return GetBaseClass().Implements(i);
            return false;
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
            // TODO: this needs to be optimized
            ClassInstance r = new ClassInstance(this, m);
            AddFields(r, m);
            // This is a last minute computation of the constructor list
            List<FunctionDefn> ctorlist = new List<FunctionDefn>(GetMethods("Constructor"));
            if (ctorlist == null)
                return r;
            ctors = new FunDefnListValue(r, "Constructor", ctorlist);
            if (ctors.Count == 0)
                return r;

            FunctionValue o = ctors.Resolve(vm, args);
            if (o == null)
                return r; // No matching constructor
            o.Apply(vm, args);
            return r;
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

            // Look for static functions to return
            if (fs.Count != 0)
                return new FunDefnListValue(Null, name, fs);
            return base.GetFieldOrMethod(name);
        }
        #endregion
    }
}
