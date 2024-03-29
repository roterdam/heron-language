﻿/// Heron language interpreter for Windows in C#
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
    /// An attribute used to identify methods and properties on a HeronValue derived type 
    /// which are to be exposed automatically to Heron. The exposed functions are managed by PrimitiveType.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property)]    
    public sealed class HeronVisible : Attribute
    {
    }

    /// <summary>
    /// This is the base class of all values that are exposed to Heron.
    /// </summary>
    public abstract class HeronValue
    {
        public static VoidValue Void = new VoidValue();
        public static NullValue Null = new NullValue();

        public HeronValue()
        {
        }

        public virtual Object Unmarshal()
        {
            return this;
        }

        public virtual bool ToBool()
        {
            throw new Exception("Cannot convert '" + ToString() + "' into System.Boolean");
        }

        public virtual int ToInt()
        {
            throw new Exception("Cannot convert '" + ToString() + "' into System.Int32");
        }

        public virtual char ToChar()
        {
            throw new Exception("Cannot convert '" + ToString() + "' into System.Char");
        }

        public virtual float ToFloat()
        {
            throw new Exception("Cannot convert '" + ToString() + "' into System.Float");
        }

        public virtual HeronValue GetAtIndex(HeronValue index)
        {
            throw new Exception("Indexing is not supported on " + ToString());
        }

        public virtual void SetAtIndex(HeronValue index, HeronValue val)
        {
            throw new Exception("Indexing is not supported on " + ToString());
        }

        /// <summary>
        /// Treats this value as a function and calls it.
        /// </summary>
        /// <param name="vm"></param>
        /// <param name="funcs"></param>
        /// <returns></returns>
        public virtual HeronValue Apply(VM vm, HeronValue[] args)
        {
            throw new Exception(ToString() + " is not recognized a function object");
        }
        
        /// <summary>
        /// Given a name returns the appropriate field (or method)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual HeronValue GetFieldOrMethod(string name)
        {
            HeronType t = Type;
            ExposedMethodValue m = t.GetMethod(name); 
            if (m != null)
                return m.CreateBoundMethod(this);
            FieldDefn f = t.GetField(name);
            if (f != null)
                return f.GetValue(this);
            return null;
        }

        /// <summary>
        /// Sets the value associated with the named field
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        public virtual void SetField(string name, HeronValue val)
        {
            HeronType t = Type;
            FieldDefn fi = t.GetField(name);
            fi.SetValue(this, val);
        }

        public virtual HeronValue InvokeUnaryOperator(VM vm, string s)
        {
            throw new Exception("unary operator '" + s + "' not supported on " + ToString());
        }

        public virtual HeronValue InvokeBinaryOperator(VM vm, OpCode opcode, HeronValue val)
        {
            throw new Exception("binary operator '" + Enum.GetName(typeof(OpCode), opcode) + "' not supported on " + ToString());
        }

        public abstract HeronType Type { get; }

        [HeronVisible]
        public override string ToString()
        {
            return base.ToString();
        }

        public IEnumerable<FieldInfo> GetInstanceFields()
        {
            return GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        }

        public IEnumerable<FieldInfo> GetTypedInstanceFields<T>()
        {
            foreach (FieldInfo fi in GetInstanceFields())
                if (fi.FieldType.Equals(typeof(T)))
                    yield return fi;
        }

        public bool SupportsFunction(FunctionDefn f)
        {
            HeronValue v = GetFieldOrMethod(f.name);
            if (v == null)
                return false;

            if (v is DotNetMethodValue)
            {
                var emv = v as DotNetMethodValue;
                return f.Matches(emv.GetMethodInfo());
            }
            else if (v is FunDefnListValue)
            {
                var fdlv = v as FunDefnListValue;
                foreach (FunctionDefn fd in fdlv.GetDefns())
                    if (fd.Matches(f))
                        return true;
            }

            // Unrecognized value type.
            return false;
        }

        public virtual HeronValue As(HeronType t)
        {
            if (t.IsAssignableFrom(Type))
                return this;
            if (t == PrimitiveTypes.AnyType)
                return new AnyValue(this);
            if (t == PrimitiveTypes.UnknownType)
                return this;
            return null;
        }

        public virtual bool Is(HeronType t)
        {
            return As(t) != null;
        }

        public override bool Equals(Object o)
        {
            return o == this;
        }

        public override int GetHashCode()
        {            
            return base.GetHashCode();
        }
    }

    #region special values
    /// <summary>
    /// Used to represent void types, which are non-value returned from a function.
    /// </summary>
    public class VoidValue : HeronValue
    {
        public override string ToString()
        {
            return "Void";
        }

        public override HeronType Type
        {
            get { return PrimitiveTypes.VoidType; }
        }
    }

    /// <summary>
    /// Represents a null value (called NULL or nil in other languages
    /// </summary>
    public class NullValue : HeronValue
    {
        public override string ToString()
        {
            return "null";
        }

        public override HeronType Type
        {
            get { return PrimitiveTypes.NullType; }
        }

        public override bool Equals(Object x)
        {
            Debug.Assert(x is NullValue ? x == this : true);
            return x is NullValue;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class TypeValue : HeronValue
    {
        HeronType type;

        public TypeValue(HeronType t)
        {
            type = t;
        }

        public override HeronType Type
        {
            get { return PrimitiveTypes.TypeValueType; }
        }

        public override HeronValue GetFieldOrMethod(string name)
        {
            HeronValue v = type.GetField(name);
            if (v != null)
                return v;

            ExposedMethodValue m = type.GetMethod(name);
            if (m != null)
                return m.CreateBoundMethod(null);
            throw new Exception("The function '" + name + "' is not available on " + type.name);
        }
    }
    #endregion
}

