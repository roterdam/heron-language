/// Heron language interpreter for Windows in C#
/// http://www.heron-language.com
/// Copyright (c) 2009 Christopher Diggins
/// Licenced under the MIT License 1.0 
/// http://www.opensource.org/licenses/mit-license.php

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;

namespace HeronEngine
{
    /// <summary>
    /// Takes an IEnumerator instance and converts it into a HeronValue
    /// specifically: an IteratorValue
    /// </summary>
    public class DotNetEnumeratorToHeronAdapter
        : IteratorValue
    {
        IEnumerator iter;

        public DotNetEnumeratorToHeronAdapter(IEnumerator iter)
        {
            this.iter = iter;
        }

        public override bool MoveNext(VM vm)
        {
            return iter.MoveNext();
        }

        public override HeronValue GetValue(VM vm)
        {
            return DotNetObject.Marshal(iter.Current);
        }
    }

    public class DotNetMethod : HeronValue
    {
        MethodInfo mi;
        HeronValue self;

        public DotNetMethod(MethodInfo mi, HeronValue self)
        {
            this.mi = mi;
            this.self = self;
        }

        public override HeronValue Apply(VM vm, HeronValue[] args)
        {
            Object[] objs = HeronDotNet.ObjectsToDotNetArray(args);
            Object r = mi.Invoke(self, objs);
            return DotNetObject.Marshal(r);
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.ExternalMethodType;
        }
    }

    public class DotNetObject : HeronValue
    {
        Object obj;
        HeronType type;

        /// <summary>
        /// This is private because you should used DotNetObject.Marshal instead
        /// </summary>
        /// <param name="obj"></param>
        private DotNetObject(Object obj)
        {
            this.obj = obj;
            type = new DotNetClass(null, this.obj.GetType().Name, this.obj.GetType());
        }

        /// <summary>
        /// Creates a Heron object from a System (.NET) object
        /// If it is a primitive, this will convert to the Heron primitives
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static HeronValue Marshal(Object o)
        {
            return HeronDotNet.DotNetToHeronObject(o);
        }

        public static Object Unmarshal(Type t, HeronValue v)
        {
            Object o = v.ToSystemObject();
            Trace.Assert(o.GetType().Equals(t));
            return o;
        }

        internal static HeronValue CreateDotNetObjectNoMarshal(Object o)
        {
            return new DotNetObject(o);
        }

        public override Object ToSystemObject()
        {
            return obj;
        }

        public override string ToString()
        {
            return obj.ToString();
        }

        public Type GetSystemType()
        {
            return obj.GetType();
        }

        public override HeronValue GetFieldOrMethod(string name)
        {
            Type type = GetSystemType();

            // We have to first look to see if there are static fields
            FieldInfo[] fis = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetField);
            foreach (FieldInfo fi in fis)
                if (fi.Name == name)
                    return DotNetObject.Marshal(fi.GetValue(obj));

            // Look for methods
            MethodInfo[] mis = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod);
            if (mis.Length != 0)
                return new DotNetMethodGroup(this, name);

            // No static field or method found.
            // TODO: could eventually support property.
            throw new Exception("Could not find field, or static method " + name);
        }


        public override HeronType GetHeronType()
        {
            return type;
        }
    }

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
                    // This is Her because a string implements IEnumerable
                    if (o is IEnumerable)
                        return new DotNetEnumeratorToHeronAdapter((o as IEnumerable).GetEnumerator());
                    else
                        return DotNetObject.CreateDotNetObjectNoMarshal(o);
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

    /// <summary>
    /// Exposes a .NET class to Heron
    /// </summary>
    public class DotNetClass : HeronType
    {
        public DotNetClass(HeronModule m, string name, Type type)
            : base(m, type, name)
        {
        }

        public DotNetClass(HeronModule m, Type type)
            : base(m, type, type.Name)
        {
        }

        public override HeronValue Instantiate(VM vm, HeronValue[] args)
        {
            Object[] objs = HeronDotNet.ObjectsToDotNetArray(args);
            Object o = GetSystemType().InvokeMember(null, BindingFlags.Instance | BindingFlags.Public 
                | BindingFlags.Default | BindingFlags.CreateInstance, null, null, objs);
            if (o == null)
                throw new Exception("Unable to construct " + name);
            return DotNetObject.Marshal(o);
        }

        /// <summary>
        /// Returns the value of a static field, or a method group.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override HeronValue GetFieldOrMethod(string name)
        {
            // We have to first look to see if there are static fields
            FieldInfo[] fis = GetSystemType().GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.GetField);
            foreach (FieldInfo fi in fis)
                if (fi.Name == name)
                    return DotNetObject.Marshal(fi.GetValue(null));

            // Look for methods
            MethodInfo[] mis = GetSystemType().GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod);
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
            return (obj as DotNetClass).GetSystemType().Equals(GetSystemType());
        }

        public override int GetHashCode()
        {
            return GetSystemType().GetHashCode();
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

        public override HeronValue Apply(VM vm, HeronValue[] args)
        {
            Object[] os = HeronDotNet.ObjectsToDotNetArray(args);
            Object o = self.GetSystemType().InvokeMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, self.ToSystemObject(), os);
            return DotNetObject.Marshal(o);
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.ExternalMethodListType;
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

        public override HeronValue Apply(VM vm, HeronValue[] args)
        {
            Object[] os = HeronDotNet.ObjectsToDotNetArray(args);
            Object o = self.GetSystemType().InvokeMember(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, os);
            return DotNetObject.Marshal(o);
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.ExternalStaticMethodListType;
        }
    }

    /// <summary>
    /// Exposes a method from a Heron primitive type to Heron
    /// </summary>
    public class ExposedMethod : Method
    {
        MethodInfo method;

        public ExposedMethod(MethodInfo mi)
        {
            method = mi;
        }

        public String Name
        {
            get { return method.Name; }
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.PrimitiveMethodType;
        }

        public override HeronValue Invoke(VM vm, HeronValue self, HeronValue[] args)
        {
            int nParams = method.GetParameters().Length;
            if (nParams != args.Length)
                throw new Exception("Insufficient number of arguments " + args.Length + " expected " + nParams);
            for (int i = 0; i < nParams; ++i )
            {
                ParameterInfo pi = method.GetParameters()[i];
                if (!pi.ParameterType.IsAssignableFrom(args[i].GetType()))
                {
                    String msg = "Cannot convert parameter " + i + " from a " 
                        + pi.ParameterType.Name + " to a " + args[i].GetType().Name;
                    throw new Exception(msg);
                }
            }
            return DotNetObject.Marshal(method.Invoke(self, args));
        }
    }
}
