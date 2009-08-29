using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace HeronEngine
{
    /// <summary>
    /// See also: HeronClass
    /// </summary>
    public abstract class HeronType : HeronObject 
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
        public abstract HeronObject Instantiate(Environment env, HeronObject[] args);

        public HeronObject Instantiate(Environment env)
        {
            return Instantiate(env, new HeronObject[] { });
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

        public virtual IEnumerable<HeronFunction> GetMethods()
        {
            return new List<HeronFunction>();
        }
        
        public IEnumerable<HeronFunction> GetMethods(string name)
        {
            foreach (HeronFunction f in GetMethods())
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
            return HeronPrimitiveTypes.TypeType;
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

        public override HeronObject Instantiate(Environment env, HeronObject[] args)
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

    public class DotNetClass : HeronType
    {
        Type type;

        public DotNetClass(HeronModule m, string name, Type type)
            : base(m, name)
        {
            this.type = type;
        }

        public DotNetClass(HeronModule m, Type type)
            : base(m, type.Name)
        {
            this.type = type;
        }

        public override HeronObject Instantiate(Environment env, HeronObject[] args)
        {
            Object[] objs = HeronDotNet.ObjectsToDotNetArray(args);
            Object o = type.InvokeMember(null, BindingFlags.Instance | BindingFlags.Public | BindingFlags.Default | BindingFlags.CreateInstance, null, null, objs);
            if (o == null)
                throw new Exception("Unable to construct " + name);
            return DotNetObject.Marshal(o);
        }

        public Type GetSystemType()
        {
            return type;
        }

        /// <summary>
        /// Returns the value of a static field, or a method group.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override HeronObject GetFieldOrMethod(string name)
        {
            // We have to first look to see if there are static fields
            FieldInfo[] fis = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.GetField);          
            foreach (FieldInfo fi in fis) 
                if (fi.Name == name)
                   return DotNetObject.Marshal(fi.GetValue(null));
            
            // Look for methods
            MethodInfo[] mis = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod);
            if (mis.Length != 0)
                return new DotNetStaticMethodGroup(this, name);

            // No static field or method found.
            // TODO: could eventually support property.
            throw new Exception("Could not find static field, or static method " + name + " in object " + this.name);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is DotNetClass))
                return false;
            return (obj as DotNetClass).type.Equals(type);
        }

        public override int GetHashCode()
        {
            return type.GetHashCode();
        }
    }
}


