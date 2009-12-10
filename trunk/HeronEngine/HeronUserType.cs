/// Heron language interpreter for Windows in C#
/// http://www.heron-language.com
/// Copyright (c) 2009 Christopher Diggins
/// Licenced under the MIT License 1.0 
/// http://www.opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace HeronEngine
{
    public abstract class HeronUserType : HeronType
    {
        public HeronUserType(HeronModule m, Type t, string name)
            : base(m, t, name)
        {
        }

        [HeronVisible]
        public abstract IEnumerable<FunctionDefn> GetAllMethods();

        [HeronVisible]
        public IEnumerable<FunctionDefn> GetMethods(string name)
        {
            foreach (FunctionDefn f in GetAllMethods())
                if (f.name == name)
                    yield return f;
        }
    }   

    /// <summary>
    /// Represents the definition of a member field of a Heron class. 
    /// Like "MethodInfo" in C#.
    /// </summary>
    public class FieldDefn : HeronValue
    {
        [HeronVisible]
        public string name;
        [HeronVisible]
        public HeronType type = PrimitiveTypes.AnyType;

        public void ResolveTypes()
        {
            if (type is UnresolvedType)
                type = (type as UnresolvedType).Resolve();
        }

        public virtual HeronValue GetValue(HeronValue self)
        {
            return self.GetFieldOrMethod(name);        
        }

        public virtual void SetValue(HeronValue self, HeronValue x)
        {
            self.SetField(name, x);
        }

        public override string ToString()
        {
            return name + " : " + type.ToString();
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.FieldDefnType;
        }
    }

    /// <summary>
    /// Represents a field of a class derived from HeronValue
    /// </summary>
    public class ExposedField : FieldDefn
    {
        private FieldInfo fi;

        public ExposedField(FieldInfo fi)
        {
            this.name = fi.Name;
            this.type = new DotNetClass(null, fi.FieldType);
            this.fi = fi;
        }

        public override HeronValue GetValue(HeronValue self)
        {
            return DotNetObject.Marshal(fi.GetValue(self));
        }

        public override void SetValue(HeronValue self, HeronValue x)
        {
            fi.SetValue(self, DotNetObject.Unmarshal(fi.FieldType, x));
        }

        public override string ToString()
        {
            return fi.Name + " : " + fi.FieldType.ToString();
        }
    }

    /// <summary>
    /// An instance of an interface type.
    /// </summary>
    public class HeronInterface : HeronUserType
    {
        List<FunctionDefn> methods = new List<FunctionDefn>();
        List<HeronType> basetypes = new List<HeronType>();

        public HeronInterface(HeronModule m, string name)
            : base(m, typeof(HeronInterface), name)
        {
        }
        public void ResolveTypes()
        {
            for (int i = 0; i < basetypes.Count; ++i)
            {
                HeronType t = basetypes[i];
                if (t is UnresolvedType)
                    basetypes[i] = (t as UnresolvedType).Resolve();
            }

            foreach (FunctionDefn f in GetAllMethods())
                f.ResolveTypes();
        }
        public void AddBaseInterface(HeronType t)
        {
            basetypes.Add(t);
        }
        public override HeronValue Instantiate(VM vm, HeronValue[] args)
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
        public IEnumerable<HeronInterface> GetInheritedInterfaces()
        {
            foreach (HeronInterface i in basetypes)
                yield return i;
        }

        [HeronVisible]
        public IEnumerable<FunctionDefn> GetDeclaredMethods()
        {
            return methods;
        }

        [HeronVisible]
        public IEnumerable<FunctionDefn> GetInheritedMethods()
        {
            foreach (HeronInterface i in GetInheritedInterfaces())
                foreach (FunctionDefn f in i.GetAllMethods())
                    yield return f;
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

        public bool InheritsFrom(HeronInterface i)
        {
            string s = i.name;
            if (s == name)
                return true;
            foreach (HeronInterface bi in basetypes)
                if (bi.InheritsFrom(i))
                    return true;
            return false;
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.InterfaceType;
        }
    }

    /// <summary>
    /// An instance of an enum type.
    /// </summary>
    public class HeronEnum : HeronUserType
    {
        List<String> values = new List<String>();

        public HeronEnum(HeronModule m, string name)
            : base(m, typeof(HeronEnum), name)
        {
        }

        public override IEnumerable<FunctionDefn> GetAllMethods()
        {
            yield break;
        }

        public override HeronValue Instantiate(VM vm, HeronValue[] args)
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
            HeronEnum e = obj as HeronEnum;
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

    /// <summary>
    /// An instance of a class type.
    /// </summary>
    public class HeronClass : HeronUserType
    {
        #region fields
        List<FunctionDefn> methods = new List<FunctionDefn>();
        List<FieldDefn> fields = new List<FieldDefn>();
        HeronType baseclass = null;
        List<HeronType> interfaces = new List<HeronType>();
        FunDefnListValue ctors;
        #endregion

        #region internal function

        public HeronClass(HeronModule m, string name)
            : base(m, typeof(HeronClass), name)
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
                    HeronInterface hi = ht as HeronInterface;
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

        public bool VerifyImplements(HeronInterface i)
        {
            foreach (FunctionDefn f in i.GetAllMethods())
                if (!HasFunction(f))
                    return false;
            return true;
        }

        public void VerifyInterfaces()
        {
            foreach (HeronInterface i in interfaces)
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
        public IEnumerable<HeronClass> GetInheritedClasses()
        {
            if (baseclass != null)
                yield return baseclass as HeronClass;
        }

        [HeronVisible]
        public IEnumerable<HeronType> GetImplementedInterfaces()
        {
            return interfaces;
        }

        [HeronVisible]
        public HeronClass GetBaseClass()
        {
            return baseclass as HeronClass;
        }

        [HeronVisible]
        public void AddInterface(HeronType i)
        {
            if (i == null)
                throw new Exception("Cannot add 'null' as an interface");
            interfaces.Add(i);
        }

        public void AddFields(ClassInstance i)
        {
            i.AddField("this", i);

            foreach (FieldDefn field in fields)
                i.AddField(field.name, null);

            if (GetBaseClass() != null)
            {
                ClassInstance b = new ClassInstance(GetBaseClass());
                GetBaseClass().AddFields(b);
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
        public bool Implements(HeronInterface i)
        {
            string s = i.name;
            foreach (HeronInterface i2 in interfaces)
                if (i2.name == s)
                    return true;
            if (GetBaseClass() != null)
                return GetBaseClass().Implements(i);
            return false;
        }

        [HeronVisible]
        public bool InheritsFrom(HeronClass c)
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
        public override HeronValue Instantiate(VM vm, HeronValue[] args)
        {
            // TODO: this needs to be optimized
            ClassInstance r = new ClassInstance(this);
            AddFields(r);
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
                foreach (FunctionDefn f in (baseclass as HeronClass).GetAllMethods())
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
                foreach (FunctionDefn f in (baseclass as HeronClass).GetAllMethods())
                    yield return f;
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.ClassType;
        }
        #endregion
    }
}
