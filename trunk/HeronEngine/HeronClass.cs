using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace HeronEngine
{
    /// <summary>
    /// Represents a member field of a class. 
    /// </summary>
    public class HeronField
    {
        public string name;
        public HeronType type = HeronPrimitiveTypes.AnyType;

        public void ResolveTypes()
        {
            if (type is UnresolvedType)
                type = (type as UnresolvedType).Resolve();
        }
    }

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

            foreach (HeronFunction f in GetMethods())
                f.ResolveTypes();
        }
        public void AddBaseInterface(HeronType t)
        {
            basetypes.Add(t);
        }
        public override HeronObject Instantiate(Environment env, HeronObject[] args)
        {
            throw new Exception("Cannot instantiate an interface");
        }
        public override IEnumerable<HeronFunction> GetMethods()
        {
            foreach (HeronFunction f in methods)
                yield return f;
            foreach (HeronInterface i in basetypes)
                foreach (HeronFunction f in i.GetMethods())
                    yield return f;
        }
        // TODO: see if I can remove all "HasMethod" calls.
        public bool HasMethod(string name)
        {
            foreach (HeronFunction f in GetMethods(name))
                return true;
            return false;
        }
        public void AddMethod(HeronFunction x)
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
    }

    public class HeronEnum : HeronType
    {
        List<String> values = new List<String>();

        public HeronEnum(HeronModule m, string name)
            : base(m, name)
        {
        }

        public override HeronObject Instantiate(Environment env, HeronObject[] args)
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

        public override HeronObject GetFieldOrMethod(string name)
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
    }

    public class HeronClass : HeronType
    {
        #region fields 
        FunctionTable methods = new FunctionTable();
        List<HeronField> fields = new List<HeronField>();
        HeronType baseclass = null;
        List<HeronType> interfaces = new List<HeronType>();
        FunctionListObject ctors;
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
            foreach (HeronFunction f in GetMethods())
                f.ResolveTypes();
        }

        internal bool VerifyImplements(HeronInterface i)
        {
            foreach (HeronFunction f in i.GetMethods())
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

        internal bool HasFunction(HeronFunction f)
        {
            foreach (HeronFunction g in GetMethods(f.name))
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
            foreach (HeronFunction f in GetMethods(name))
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
        public override HeronObject Instantiate(Environment env, HeronObject[] args)
        {
            // TODO: this needs to be optimized
            ClassInstance r = new ClassInstance(this);
            AddFields(r);
            // This is a last minute computation of the constructor list
            List<HeronFunction> ctorlist = new List<HeronFunction>(GetMethods("Constructor"));
            if (ctorlist == null)
                return r;
            ctors = new FunctionListObject(r, "Constructor", ctorlist);
            if (ctors.Count == 0)
                return r;

            FunctionObject o = ctors.Resolve(args);
            if (o == null)
                return r; // No matching constructor
            o.Apply(env, args);
            return r;
        }
        
        public IEnumerable<HeronField> GetFields()
        {
            return fields;
        }

        public void AddMethod(HeronFunction x)
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

        public FunctionListObject GetCtors()
        {
            return ctors;
        }

        public override IEnumerable<HeronFunction> GetMethods()
        {
            foreach (HeronFunction f in methods)
                yield return f;
            if (baseclass != null)
                foreach (HeronFunction f in baseclass.GetMethods())
                    yield return f;
        }

        #endregion
    }
}
    
