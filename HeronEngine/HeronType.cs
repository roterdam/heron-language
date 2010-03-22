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
    /// See also: ClassDefn. Note types are also values in Heron. 
    /// </summary>
    public class HeronType : HeronValue 
    {
        [HeronVisible] public string name = "anonymous_type";
        
        // I tried to make the next two variables [HeronVisible] that caused an infinite loop.
        // StoreExposedFunctionsAndFields
        public HeronType baseType = null;
        public ModuleDefn module = null;
        
        Type type;
        Dictionary<string, ExposedMethodValue> functions = new Dictionary<string, ExposedMethodValue>();
        Dictionary<string, ExposedField> fields = new Dictionary<string, ExposedField>();

        static Dictionary<string, Type> allTypes = new Dictionary<string,Type>();

        public HeronType(ModuleDefn m, Type t, string name)
        {
            module = m;
            type = t;
            Debug.Assert(t != null);
            this.name = name;
            StoreExposedFunctionsAndFields();
        }

        public HeronType(HeronType baseType, ModuleDefn m, Type t, string name)
        {
            module = m;
            type = t;
            this.baseType = baseType;
            Debug.Assert(t != null);
            this.name = name;
            StoreExposedFunctionsAndFields();
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

        [HeronVisible]
        public ModuleDefn GetModule()
        {
            return module;
        }

        public Type GetSystemType()
        {
            return type;
        }

        public void SetModule(ModuleDefn m)
        {
            module = m;
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.TypeType;
        }

        /// <summary>
        /// Iterates over 'HeronVisible' labeled fields and values 
        /// of the target object and exposes them. 
        /// </summary>
        private void StoreExposedFunctionsAndFields()
        {
            if (type == null)
                return;
            if (type == typeof(UnresolvedType))
                return;
            if (!typeof(HeronValue).IsAssignableFrom(type))
                return;

            foreach (MethodInfo mi in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                object[] attrs = mi.GetCustomAttributes(typeof(HeronVisible), true);
                if (attrs.Length > 0)
                    StoreExposedFunction(mi);
            }

            foreach (FieldInfo fi in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                object[] attrs = fi.GetCustomAttributes(typeof(HeronVisible), true);
                if (attrs.Length > 0)
                    StoreExposedField(fi);
            }
        }

        private void StoreExposedFunction(MethodInfo mi)
        {
            ExposedMethodValue m = new ExposedMethodValue(mi);
            functions.Add(m.Name, m);
        }

        private void StoreExposedField(FieldInfo fi)
        {
            ExposedField f = new ExposedField(fi);
            fields.Add(f.name, f);
        }

        public IEnumerable<ExposedField> GetExposedFields()
        {
            return fields.Values;
        }

        public IEnumerable<ExposedMethodValue> GetExposedMethods()
        {
            return functions.Values; 
        }

        /// <summary>
        /// This is overriden in the variou user types. Only classes and primitives
        /// can be instantiated (not enums or interfaces).
        /// </summary>
        /// <param name="vm"></param>
        /// <param name="funcs"></param>
        /// <param name="mi"></param>
        /// <returns></returns>
        public virtual HeronValue Instantiate(VM vm, HeronValue[] args, ModuleInstance m)
        {
            if (args.Length != 0)
                throw new Exception("arguments not supported when instantiating primitive type " + name);

            if (type == null)
                throw new Exception("type " + name + " can't be instantiated");

            Object r = type.InvokeMember(null, System.Reflection.BindingFlags.CreateInstance, null, null, new Object[] { });
            return r as HeronValue;
        }

        #region heron visible functions
        [HeronVisible]
        public virtual ExposedMethodValue GetMethod(string name)
        {
            if (!functions.ContainsKey(name))
                return null;
            return functions[name];
        }

        [HeronVisible]
        public virtual FieldDefn GetField(string name)
        {
            if (!fields.ContainsKey(name))
                return null;
            return fields[name];
        }

        [HeronVisible]
        public override bool Equals(object obj)
        {
            HeronType t = obj as HeronType;
            if (t == null) return false;
            return name == t.name;
        }

        [HeronVisible]
        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        [HeronVisible]
        public override string ToString()
        {
            return name;
        }

        [HeronVisible]
        public string GetName()
        {
            return name;
        }
        #endregion

        public virtual int GetHierarchyDepth()
        {
            return 1;
        }

        public virtual HeronType Resolve(ModuleDefn m)
        {
            return this;
        }

        public bool IsAssignableFrom(HeronType type)
        {
            if (Equals(type)) return true;
            if (type.baseType != null)
                return IsAssignableFrom(type.baseType);
            else
                return false;
        }
    }

    /// <summary>
    /// A place-holder for types after parsing. Once parsing is complete, any occurence of an
    /// instance of UnresolvedType should be replaced with the correct type. To do this, requires
    /// all modules to be parsed, so it has to be done after parsing. There are probably more 
    /// elegant solutions available, but this is the best I could come up with. It does not 
    /// require a lot of code, and errors are easy to detect.
    /// </summary>
    public class UnresolvedType : HeronType
    {
        public UnresolvedType(string name)
            : base(null, typeof(UnresolvedType), name)
        {
        }

        public override HeronValue Instantiate(VM vm, HeronValue[] args, ModuleInstance m)
        {
            throw new Exception("Type '" + name + "' was not resolved.");
        }

        public override HeronType Resolve(ModuleDefn m)
        {
            HeronType r = m.FindType(name);
            if (r == null)
                r = m.GetGlobal().FindType(name);
            if (r == null)
                throw new Exception("Could not resolve type " + name);
            if (r.name != name)
                throw new Exception("Internal error during type resolution of " + name);
            return r;
        }
    }

    /// <summary>
    /// Used to identify the type of values that are part of the code model
    /// </summary>
    public class HeronCodeModelType : HeronType
    {
        public HeronCodeModelType(Type t)
            : base(null, t, t.Name)
        {
        }
    }
}


