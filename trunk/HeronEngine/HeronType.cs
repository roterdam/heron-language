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
    public class HeronType : HeronValue 
    {
        [HeronVisible]
        public string name = "anonymous_type";

        HeronModule module;
        Type type;
        Dictionary<string, Method> functions = new Dictionary<string, Method>();
        Dictionary<string, ExposedField> fields = new Dictionary<string, ExposedField>();

        public HeronType(HeronModule m, Type t, string name)
        {
            module = m;
            type = t;
            Trace.Assert(t != null);
            this.name = name;
            StoreExposedFunctionsAndFields();
        }

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

        [HeronVisible]
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

        [HeronVisible]
        public HeronModule GetModule()
        {
            return module;
        }

        public Type GetSystemType()
        {
            return type;
        }

        public void SetModule(HeronModule m)
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
            ExposedMethod m = new ExposedMethod(mi);
            functions.Add(m.Name, m);
        }

        private void StoreExposedField(FieldInfo fi)
        {
            ExposedField f = new ExposedField(fi);
            fields.Add(f.name, f);
        }

        public virtual HeronValue Instantiate(VM vm, HeronValue[] args)
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
        public virtual Method GetMethod(string name)
        {
            if (!functions.ContainsKey(name))
                return null;
            return functions[name];
        }

        [HeronVisible]
        public virtual Field GetField(string name)
        {
            if (!fields.ContainsKey(name))
                return null;
            return fields[name];
        }

        [HeronVisible]
        public override bool Equals(object obj)
        {
            if (!(obj is HeronType))
                return false;
            return name == (obj as HeronType).name;
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
        public StringValue GetName()
        {
            return new StringValue(name);
        }
        #endregion
    }

    /// <summary>
    /// A place-holder for types during parsing. 
    /// Should be replaced with an actual type during run-time
    /// </summary>
    public class UnresolvedType : HeronType
    {
        public UnresolvedType(string name, HeronModule m)
            : base(m, typeof(UnresolvedType), name)
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
}


