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
    /// <summary>
    /// See also: HeronClass. Note types are also values in Heron. 
    /// </summary>
    public abstract class HeronType : HeronValue 
    {
        public string name = "anonymous_type";

        HeronModule module;

        public HeronType(HeronModule m, string name)
        {
            module = m;
            this.name = name;
        }

        /// <summary>
        /// Creates an instance of the type.
        /// </summary>
        /// <param name="env"></param>
        /// <returns></returns>
        public abstract HeronValue Instantiate(VM vm, HeronValue[] args);

        /// <summary>
        /// Creates an instance of the type, without arguments. 
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        public HeronValue Instantiate(VM vm)
        {
            return Instantiate(vm, new HeronValue[] { });
        }

        /// <summary>
        /// A utility function for converting type names with template arguments into their base names
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string StripTemplateArgs(string s)
        {
            int n = s.IndexOf('<');
            if (n == 0)
                throw new Exception("illegal type name " + s);
            if (n < 0)
                return s;
            return s.Substring(0, n);
        }

        public virtual IEnumerable<FunctionDefinition> GetMethods()
        {
            return new List<FunctionDefinition>();
        }
        
        public IEnumerable<FunctionDefinition> GetMethods(string name)
        {
            foreach (FunctionDefinition f in GetMethods())
                if (f.name == name)
                    yield return f;
        }

        public HeronModule GetModule()
        {
            return module;
        }

        public void SetModule(HeronModule m)
        {
            module = m;
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.TypeType;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is HeronType))
                return false;
            return name == (obj as HeronType).name;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        public override string ToString()
        {
            return name;
        }

        [HeronVisible]
        public StringValue GetName()
        {
            return new StringValue(name);
        }
    }

    /// <summary>
    /// A place-holder for types during parsing. 
    /// Should be replaced with an actual type during run-time
    /// </summary>
    public class UnresolvedType : HeronType
    {
        public UnresolvedType(string name, HeronModule m)
            : base(m, name)
        {
        }

        public override HeronValue Instantiate(VM vm, HeronValue[] args)
        {
            throw new Exception("Type '" + name + "' was not resolved.");
        }

        public HeronType Resolve()
        {
            HeronType r = GetModule().FindType(name);
            if (r == null)
                r = GetModule().GetGlobal().FindType(name);
            if (r == null)
                throw new Exception("Could not resolve type " + name);
            if (r.name != name)
                throw new Exception("Internal error during type resolution of " + name);
            return r;
        }
    }

    /// <summary>
    /// Represents a member field of a class. 
    /// </summary>
    public class HeronField
    {
        public string name;
        public HeronType type = PrimitiveTypes.AnyType;

        public void ResolveTypes()
        {
            if (type is UnresolvedType)
                type = (type as UnresolvedType).Resolve();
        }
    }

    /// <summary>
    /// An instance of an interface type.
    /// </summary>
    public class HeronInterface : HeronType
    {
        FunctionTable methods = new FunctionTable();
        List<HeronType> basetypes = new List<HeronType>();

        public HeronInterface(HeronModule m, string name)
            : base(m, name)
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

            foreach (FunctionDefinition f in GetMethods())
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
        public override IEnumerable<FunctionDefinition> GetMethods()
        {
            foreach (FunctionDefinition f in methods)
                yield return f;
            foreach (HeronInterface i in basetypes)
                foreach (FunctionDefinition f in i.GetMethods())
                    yield return f;
        }
        // TODO: see if I can remove all "HasMethod" calls.
        public bool HasMethod(string name)
        {
            foreach (FunctionDefinition f in GetMethods(name))
                return true;
            return false;
        }
        public void AddMethod(FunctionDefinition x)
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
    public class HeronEnum : HeronType
    {
        List<String> values = new List<String>();

        public HeronEnum(HeronModule m, string name)
            : base(m, name)
        {
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
                throw new Exception(name + " is not a member of " + this.name);
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
    public class HeronClass : HeronType
    {
        #region fields
        FunctionTable methods = new FunctionTable();
        List<HeronField> fields = new List<HeronField>();
        HeronType baseclass = null;
        List<HeronType> interfaces = new List<HeronType>();
        FunctionListValue ctors;
        #endregion

        #region internal function

        internal HeronClass(HeronModule m, string name)
            : base(m, name)
        {
        }

        internal void ResolveTypes()
        {
            if (baseclass != null)
                if (baseclass is UnresolvedType)
                    baseclass = (baseclass as UnresolvedType).Resolve();
            for (int i = 0; i < interfaces.Count; ++i)
            {
                HeronType t = interfaces[i];
                if (t is UnresolvedType)
                    interfaces[i] = (t as UnresolvedType).Resolve();
            }
            foreach (HeronField f in GetFields())
                f.ResolveTypes();
            foreach (FunctionDefinition f in GetMethods())
                f.ResolveTypes();
        }

        internal bool VerifyImplements(HeronInterface i)
        {
            foreach (FunctionDefinition f in i.GetMethods())
                if (!HasFunction(f))
                    return false;
            return true;
        }

        internal void VerifyInterfaces()
        {
            foreach (HeronInterface i in interfaces)
                if (!VerifyImplements(i))
                    throw new Exception("Class '" + name + "' does not implement the interface '" + i.name + "'");
        }

        internal bool HasFunction(FunctionDefinition f)
        {
            foreach (FunctionDefinition g in GetMethods(f.name))
                if (g.Matches(f))
                    return true;
            return false;
        }

        internal void SetBaseClass(HeronType c)
        {
            baseclass = c;
        }

        public HeronClass GetBaseClass()
        {
            return baseclass as HeronClass;
        }

        internal void AddInterface(HeronType i)
        {
            interfaces.Add(i);
        }

        internal void AddFields(ClassInstance i)
        {
            i.AddField("this", i);

            foreach (HeronField field in fields)
                i.AddField(field.name, null);

            if (GetBaseClass() != null)
            {
                ClassInstance b = new ClassInstance(GetBaseClass());
                GetBaseClass().AddFields(b);
                i.AddField("base", b);
            }
        }

        internal bool HasMethod(string name)
        {
            foreach (FunctionDefinition f in GetMethods(name))
                return true;
            return false;
        }

        internal bool Implements(HeronInterface i)
        {
            string s = i.name;
            foreach (HeronInterface i2 in interfaces)
                if (i2.name == s)
                    return true;
            if (GetBaseClass() != null)
                return GetBaseClass().Implements(i);
            return false;
        }

        internal bool InheritsFrom(HeronClass c)
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
            List<FunctionDefinition> ctorlist = new List<FunctionDefinition>(GetMethods("Constructor"));
            if (ctorlist == null)
                return r;
            ctors = new FunctionListValue(r, "Constructor", ctorlist);
            if (ctors.Count == 0)
                return r;

            FunctionValue o = ctors.Resolve(vm, args);
            if (o == null)
                return r; // No matching constructor
            o.Apply(vm, args);
            return r;
        }

        public IEnumerable<HeronField> GetFields()
        {
            return fields;
        }

        public void AddMethod(FunctionDefinition x)
        {
            methods.Add(x);
        }

        public void AddField(HeronField x)
        {
            fields.Add(x);
        }

        public HeronField GetField(string s)
        {
            foreach (HeronField f in fields)
                if (f.name == s)
                    return f;
            return null;
        }

        public FunctionListValue GetCtors()
        {
            return ctors;
        }

        public override IEnumerable<FunctionDefinition> GetMethods()
        {
            foreach (FunctionDefinition f in methods)
                yield return f;
            if (baseclass != null)
                foreach (FunctionDefinition f in baseclass.GetMethods())
                    yield return f;
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.ClassType;
        }
        #endregion
    }
}


