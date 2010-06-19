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
    /// An instance of an interface type.
    /// </summary>
    public class InterfaceDefn : HeronUserType
    {
        List<FunctionDefn> methods = new List<FunctionDefn>();
        List<TypeRef> basetypes = new List<TypeRef>();

        public InterfaceDefn(ModuleDefn m, string name)
            : base(m, typeof(InterfaceDefn), name)
        {
        }
        public void ResolveTypes(ModuleDefn global, ModuleDefn m)
        {
            foreach (TypeRef tr in basetypes)
                tr.Resolve(global, m);

            foreach (FunctionDefn f in GetAllMethods())
                f.ResolveTypes(global, m);
        }
        public void AddBaseInterface(TypeRef t)
        {
            basetypes.Add(t);
        }
        public override HeronValue Instantiate(VM vm, HeronValue[] args, ModuleInstance m)
        {
            throw new Exception("Cannot instantiate an interface");
        }

        [HeronVisible]
        public override IEnumerable<FunctionDefn> GetAllMethods()
        {
            foreach (FunctionDefn f in GetDeclaredMethods())
                yield return f;
            foreach (FunctionDefn f in GetInheritedMethods())
                yield return f;
        }

        [HeronVisible]
        public IEnumerable<InterfaceDefn> GetInheritedInterfaces()
        {
            foreach (TypeRef tr in basetypes)
            {
                InterfaceDefn id = tr.type as InterfaceDefn;
                if (id == null)
                    throw new Exception("Interface has an illegal base type");
                yield return id;
            }
        }

        [HeronVisible]
        public IEnumerable<FunctionDefn> GetDeclaredMethods()
        {
            return methods;
        }

        [HeronVisible]
        public IEnumerable<FunctionDefn> GetInheritedMethods()
        {
            foreach (InterfaceDefn i in GetInheritedInterfaces())
                foreach (FunctionDefn f in i.GetAllMethods())
                    yield return f;
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


        // TODO: see if I can remove all "HasMethod" calls.
        public bool HasMethod(string name)
        {
            foreach (FunctionDefn f in GetMethods(name))
                return true;
            return false;
        }
        public void AddMethod(FunctionDefn x)
        {
            methods.Add(x);
        }

        public bool InheritsFrom(InterfaceDefn i)
        {
            string s = i.name;
            if (s == name)
                return true;
            foreach (TypeRef tr in basetypes)
            {
                InterfaceDefn id = tr.type as InterfaceDefn;
                if (id == null)
                    throw new Exception("Could not find base interface type " + tr.name);
                if (id.InheritsFrom(i))
                    return true;
            }
            return false;
        }

        public override HeronType Type
        {
            get { return PrimitiveTypes.InterfaceType; }
        }

        public override int GetHierarchyDepth()
        {
            int r = 1;
            foreach (TypeRef tr in basetypes)
            {
                HeronType t = tr.type;
                int tmp = t.GetHierarchyDepth() + 1;
                if (tmp > r)
                    r = tmp;
            }
            return r;
        }
    }
}
