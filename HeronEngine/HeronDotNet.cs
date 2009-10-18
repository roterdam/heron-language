using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace HeronEngine
{
    /// <summary>
    /// This class is used for marshaling .NET values
    /// to and from Heron value.
    /// </summary>
    public static class HeronDotNet
    {
        public static string DotNetTypeToHeronType(string s)
        {
            switch (s)
            {
                case "int": return "Int";
                case "char": return "Char";
                case "float": return "Float";
                case "bool": return "Bool";
                case "string": return "String";
                case "Object": return "Any";
                default: return s;                    
            }
        }

        public static string DotNetToHeronTypeString(Type t)
        {
            string s = DotNetTypeToHeronType(t.Name);

            if (t.IsArray)
                return "Array<" + s + ">";

            if (t.IsGenericType)
            {
                Type[] ts = t.GetGenericArguments();
                if (ts.Length == 0)
                    return s;
                s += "<";
                for (int i = 0; i < ts.Length; ++i)
                {
                    if (i > 0) s += ", ";
                    s += DotNetToHeronTypeString(ts[i]);
                }
                s += ">";
            }

            return s;
        }

        public static HeronValue DotNetToHeronObject(Object o)
        {
            if (o == null)
                return HeronValue.Null;
            if (o is HeronValue)
                return o as HeronValue;

            Type t = o.GetType();
            
            if (t.IsArray)
            {
                HeronCollection c = new HeronCollection();
                Array a = o as Array;
                foreach (Object e in a)
                    c.Add(DotNetToHeronObject(e));
                return DotNetObject.CreateDotNetObjectNoMarshal(c);
            }
            else
            {
                switch (o.GetType().Name)
                {
                    case "Single":
                        return new FloatValue((float)o);
                    case "Double":
                        double d = (double)o;
                        // TEMP: Downcasts doubles to floats for now.
                        return new FloatValue((float)d);
                    case "Int32":
                        return new IntValue((int)o);
                    case "Char":
                        return new CharValue((char)o);
                    case "String":
                        return new StringValue((string)o);
                    case "Boolean":
                        return new BoolValue((bool)o);
                    default:
                        return DotNetObject.CreateDotNetObjectNoMarshal(o);
                }
            }
        }

        public static Object[] ObjectsToDotNetArray(HeronValue[] array)
        {
            Object[] r = new Object[array.Length];
            for (int i = 0; i < array.Length; ++i)
            {
                if (array[i] == null)
                    r[i] = null;
                else
                    r[i] = array[i].ToSystemObject();
            }
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

        public override HeronValue Instantiate(HeronExecutor vm, HeronValue[] args)
        {
            Object[] objs = HeronDotNet.ObjectsToDotNetArray(args);
            Object o = type.InvokeMember(null, BindingFlags.Instance | BindingFlags.Public 
                | BindingFlags.Default | BindingFlags.CreateInstance, null, null, objs);
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
        public override HeronValue GetFieldOrMethod(string name)
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

    /// <summary>
    /// In .NET methods are overloaded, so resolve a method name on a .NET object
    /// yields a group of methods. This class stores the object and the method
    /// name for invocation.
    /// </summary>
    public class DotNetMethodGroup : HeronValue
    {
        DotNetObject self;
        string name;

        public DotNetMethodGroup(DotNetObject self, string name)
        {
            this.self = self;
            this.name = name;
        }

        public override HeronValue Apply(HeronExecutor vm, HeronValue[] args)
        {
            Object[] os = HeronDotNet.ObjectsToDotNetArray(args);
            Object o = self.GetSystemType().InvokeMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, self.ToSystemObject(), os);
            return DotNetObject.Marshal(o);
        }

        public override HeronType GetHeronType()
        {
            return HeronPrimitiveTypes.ExternalMethodListType;
        }
    }

    /// <summary>
    /// Very similar to DotNetMethodGroup, except only static functions are bound.
    /// </summary>
    public class DotNetStaticMethodGroup : HeronValue
    {
        DotNetClass self;
        string name;

        public DotNetStaticMethodGroup(DotNetClass self, string name)
        {
            this.self = self;
            this.name = name;
        }

        public override HeronValue Apply(HeronExecutor vm, HeronValue[] args)
        {
            Object[] os = HeronDotNet.ObjectsToDotNetArray(args);
            Object o = self.GetSystemType().InvokeMember(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, os);
            return DotNetObject.Marshal(o);
        }

        public override HeronType GetHeronType()
        {
            return HeronPrimitiveTypes.ExternalStaticMethodListType;
        }
    }
}
