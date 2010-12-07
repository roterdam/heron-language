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
            type = DotNetClass.Create(this.obj.GetType());
        }

        /// <summary>
        /// Creates a Heron object from a System (.NET) object
        /// If it is a primitive, this will convert to the Heron primitives
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static HeronValue Marshal(Object o)
        {
            return HeronDotNet.Marshal(o);
        }

        /// <summary>
        /// Converts from a HeronValue to a specific System.Net type
        /// </summary>
        /// <param name="t"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Object Unmarshal(Type t, HeronValue v)
        {
            Object o = v.Unmarshal();
            Debug.Assert(o.GetType().Equals(t));
            return o;
        }

        internal static HeronValue CreateDotNetObjectNoMarshal(Object o)
        {
            return new DotNetObject(o);
        }

        public override Object Unmarshal()
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

            // We have to first look to see if there are static exposedFields
            FieldInfo[] fis = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetField);
            foreach (FieldInfo fi in fis)
                if (fi.Name == name)
                    return DotNetObject.Marshal(fi.GetValue(obj));

            // Look for methods
            MethodInfo[] mis = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod);
            if (mis.Length != 0)
                return new DotNetMethodGroup(this, name);

            return null;
        }


        public override HeronType Type
        {
            get { return type; }
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
                case "Object": return "Unknown";
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

        public static HeronValue Marshal(Object o)
        {
            if (o == null)
                return HeronValue.Null;

            HeronValue ohv = o as HeronValue;
            if (ohv != null)
                return ohv;

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
                    IList ilist = o as IList;
                    if (ilist != null)
                        return new DotNetList(ilist);
                    IEnumerable ie = o as IEnumerable;
                    if (ie != null)
                        return new ListToIterValue(ie, PrimitiveTypes.AnyType);
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
                    r[i] = array[i].Unmarshal();
            }
            return r;
        }
    }

    /// <summary>
    /// Exposes a .NET class to Heron
    /// </summary>
    public class DotNetClass : HeronType
    {
        static Dictionary<Type, DotNetClass> types = new Dictionary<Type, DotNetClass>();

        /// <summary>
        /// Assures that only one DotNetClass instance is created for each 
        /// exposed type. Calling Create multiple times with the same arguments, will return 
        /// a previously created DotNetClass.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static DotNetClass Create(string name, Type t)
        {
            if (!types.ContainsKey(t))
                types.Add(t, new DotNetClass(name, t));
            return types[t];
        }

        public static DotNetClass Create(Type t)
        {
            return Create(t.Name, t);
        }

        private DotNetClass(string name, Type type)
            : base(null, type, name)
        {
            if (type.BaseType != null)
                baseType = DotNetClass.Create(type.BaseType);
        }

        public override HeronValue Instantiate(VM vm, HeronValue[] args, ModuleInstance m)
        {
            Object[] objs = HeronDotNet.ObjectsToDotNetArray(args);
            Object o = GetSystemType().InvokeMember(null, BindingFlags.Instance | BindingFlags.Public 
                | BindingFlags.Default | BindingFlags.CreateInstance, null, null, objs);
            if (o == null)
                throw new Exception("Unable to construct " + name);
            return DotNetObject.Marshal(o);
        }

        /// <summary>
        /// Returns the value of a static method group.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override ExposedMethodValue GetMethod(string name)
        {
            // Look for methods
            MethodInfo[] mis = GetSystemType().GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod);
            if (mis.Length != 0)
                return new DotNetStaticMethodGroup(this, name);

            return null;
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

        public override HeronType Type
        {
            get { return PrimitiveTypes.ExternalClass; }
        }

        [HeronVisible]
        public override string ToString()
        {
            return name;
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
            Object o = self.GetSystemType().InvokeMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, self.Unmarshal(), os);
            return DotNetObject.Marshal(o);
        }

        public override HeronType Type
        {
            get { return PrimitiveTypes.ExternalMethodListType; }
        }
    }

    /// <summary>
    /// Very similar to DotNetMethodGroup, except only static functions are bound.
    /// </summary>
    public class DotNetStaticMethodGroup : ExposedMethodValue
    {
        DotNetClass self;
        string name;

        public DotNetStaticMethodGroup(DotNetClass self, string name)
        {
            this.self = self;
            this.name = name;
        }

        public override string Name
        {
            get { return name; }
        }

        public override HeronValue Invoke(VM vm, HeronValue self, HeronValue[] args)
        {
            Trace.Assert(self == null);
            Object[] os = HeronDotNet.ObjectsToDotNetArray(args);
            Object o = this.self.GetSystemType().InvokeMember(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, os);
            return DotNetObject.Marshal(o);
        }

        public override HeronType Type
        {
            get { return PrimitiveTypes.ExternalStaticMethodListType; }
        }

        [HeronVisible]
        public override string ToString()
        {
            return name;
        }
    }

    /// <summary>
    /// Exposes a method from a Heron primitive type to Heron
    /// </summary>
    public abstract class ExposedMethodValue : HeronValue
    {
        public abstract string Name { get; }
        
        public BoundMethodValue CreateBoundMethod(HeronValue self)
        {
            return new BoundMethodValue(self, this);
        }

        public override HeronType Type
        {
            get { return PrimitiveTypes.ExposedMethodType; }
        }

        public abstract HeronValue Invoke(VM vm, HeronValue self, HeronValue[] args);
    }

    public class DotNetMethodValue : ExposedMethodValue
    {
        MethodInfo method;

        public DotNetMethodValue(MethodInfo mi)
        {
            method = mi;
        }

        public override String Name
        {
            get { return method.Name; }
        }

        public override HeronValue Invoke(VM vm, HeronValue self, HeronValue[] args)
        {
            int nParams = method.GetParameters().Length;
            if (nParams != args.Length)
                throw new Exception("Incorrect number of arguments " + args.Length + " expected " + nParams);

            Object[] newArgs = new Object[args.Length];
            for (int i = 0; i < nParams; ++i )
            {
                ParameterInfo pi = method.GetParameters()[i];
                Type pt = pi.ParameterType;
                Type at = args[i].GetType();
                HeronValue hv = args[i] as HeronValue;
                if (!pt.IsAssignableFrom(at))
                {
                    if (hv == null)
                        throw new Exception("Could not cast parameter " + i + " from " + at + " to " + pt);
                    newArgs[i] = hv.Unmarshal();
                }
                else
                {
                    newArgs[i] = hv; 
                }
            }
            return DotNetObject.Marshal(method.Invoke(self, newArgs));
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(method.Name + "(");
            int n = 0;
            foreach (ParameterInfo pi in method.GetParameters())
            {
                if (n++ > 0)
                    sb.Append(", ");
                sb.Append(pi.Name + " : " + pi.ParameterType);
            }
            sb.Append(") : ");
            sb.Append(method.ReturnType.ToString());
            return sb.ToString();
        }

        public MethodInfo GetMethodInfo()
        {
            return method;
        }
    }

    /// <summary>
    /// Represents a collection which can be iterated over multiple times.
    /// </summary>
    public class DotNetList
        : SeqValue, IInternalIndexable
    {
        IList list;

        class DotNetListIteratorValue
            : IteratorValue
        {
            int n;
            DotNetList list;

            public DotNetListIteratorValue(DotNetList list)
            {
                n = -1;
                this.list = list;
            }

            public override bool MoveNext()
            {
                if (n >= list.InternalCount() - 1)
                    return false;
                n += 1;
                return true;
            }

            public override HeronValue GetValue()
            {
                return list.InternalAt(n);
            }

            public override IteratorValue Restart()
            {
                n = 0;
                return this;
            }

            public override HeronValue[] ToArray()
            {
                return list.ToArray();
            }

            public override IInternalIndexable GetIndexable()
            {
                return list.GetIndexable();
            }

            public override HeronType GetElementType()
            {
                return PrimitiveTypes.AnyType;
            }
        }

        public DotNetList(IList list)
        {
            this.list = list;
        }

        [HeronVisible]
        public void Add(HeronValue v)
        {
            list.Add(v.Unmarshal());
        }

        [HeronVisible]
        public void Prepend(HeronValue v)
        {
            list.Insert(0, v.Unmarshal());
        }

        [HeronVisible]
        public void Insert(HeronValue n, HeronValue v)
        {
            list.Insert((n as IntValue).GetValue(), v.Unmarshal());
        }

        [HeronVisible]
        public HeronValue Count()
        {
            return new IntValue(list.Count);
        }

        public override HeronType Type
        {
            get { return PrimitiveTypes.ExternalListType; }
        }

        public override HeronValue GetAtIndex(HeronValue index)
        {
            IntValue iv = index as IntValue;
            if (iv == null)
                throw new Exception("Can only use index lists using integers");
            return DotNetObject.Marshal(list[iv.GetValue()]);
        }

        public override void SetAtIndex(HeronValue index, HeronValue val)
        {
            IntValue iv = index as IntValue;
            if (iv == null)
                throw new Exception("Can only use index lists using integers");
            list[iv.GetValue()] = val.Unmarshal();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < list.Count; ++i)
            {
                if (i > Config.maxListPrintableSize)
                {
                    sb.Append("...");
                    break;
                }
                if (i > 0) sb.Append(", ");
                sb.Append(list[i].ToString());
            }
            sb.Append(']');
            return sb.ToString();
        }

        public override HeronValue[] ToArray()
        {
            HeronValue[] r = new HeronValue[list.Count];
            for (int i=0; i < list.Count; ++i)
                r[i] = DotNetObject.Marshal(list[i]);
            return r;
        }

        public override IteratorValue GetIterator()
        {
            return new DotNetListIteratorValue(this);
        }

        public override IInternalIndexable GetIndexable()
        {
            return this;
        }

        #region IInternalIndexable Members

        public int InternalCount()
        {
            return list.Count;
        }

        public HeronValue InternalAt(int n)
        {
            return DotNetObject.Marshal(list[n]);
        }

        #endregion

        public override HeronType GetElementType()
        {
            return PrimitiveTypes.AnyType;
        }
    }
}
