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

        static public Object[] HeronObjectArrayToDotNetArray(HeronObject[] array)
        {
            Object[] r = new Object[array.Length];
            for (int i = 0; i < array.Length; ++i)
            {
                r[i] = array[i].ToDotNetObject();
            }
            return r;
        }

        static public Type[] ObjectsToTypes(Object[] array)
        {
            Type[] r = new Type[array.Length];
            for (int i = 0; i < array.Length; ++i)
            {
                r[i] = array[i].GetType();
            }
            return r;
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
                case "List":
                    return new ListObject();
                default:
                    throw new Exception("Unhandled primitive type " + type);
            }
        }
    }

    public class DotNetType : HeronType
    {
        string name;
        Type type;

        public DotNetType(string name, Type type)
        {
            this.name = name;
            this.type = type;
        }

        public override HeronObject Instantiate(Environment env, HeronObject[] args)
        {
            Object[] objs = HeronType.HeronObjectArrayToDotNetArray(args);
            Type[] types = HeronType.ObjectsToTypes(objs);
            ConstructorInfo c = type.GetConstructor(types);
            if (c == null)
                throw new Exception("No constructor exists for " + name);
            Object o = c.Invoke(objs);
            if (o == null)
                throw new Exception("Unable to construct " + name);
            return new DotNetObject(o);
        }

        public override HeronObject Invoke(Environment env, string s, HeronObject self, HeronObject[] args)
        {
            Object[] objs = HeronType.HeronObjectArrayToDotNetArray(args);
            Type[] types = HeronType.ObjectsToTypes(objs);
            MethodInfo mi = type.GetMethod(s, BindingFlags.Static, null, types, null);
            if (mi == null)
                throw new Exception("unable to find static method " + s + " on the dot net class " + type.Name + " with supplied argument types");
            if (!mi.IsStatic)
                throw new Exception("method " + s + " of the dot net class " + type.Name + " is not static");
            Object r = mi.Invoke(null, objs);
            return new DotNetObject(r);
        }
    }
}
