using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace HeronEngine
{
    /// <summary>
    /// See also: HeronClass
    /// </summary>
    public abstract class HeronType : HeronObject 
    {
        public string name = "anonymous_type";

        public HeronType()
        {
        }

        /// <summary>
        /// Creates an instance of the type.
        /// </summary>
        /// <param name="env"></param>
        /// <returns></returns>
        public abstract HeronObject Instantiate(Environment env, HeronObject[] args);

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
    }

    public class HeronPrimitive : HeronType
    {
        string type;

        public HeronPrimitive(string type)
        {
            this.type = type;
        }

        public override HeronObject Instantiate(Environment env, HeronObject[] args)
        {
            if (args.Length != 0)
                throw new Exception("arguments not supported when instantiating primitives");

            switch (type)
            {
                case "Int":
                    return new IntObject();
                case "Float":
                    return new FloatObject();
                case "Char":
                    return new CharObject();
                case "String":
                    return new StringObject();
                case "Collection":
                    return DotNetObject.Marshal(new HeronCollection());
                default:
                    throw new Exception("Unhandled primitive type " + type);
            }
        }
    }

    public class DotNetClass : HeronType
    {
        Type type;

        public DotNetClass(string name, Type type)
        {
            this.name = name;
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
    }
}


