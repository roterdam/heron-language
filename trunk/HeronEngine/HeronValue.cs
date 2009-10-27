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
    public abstract class HeronValue
    {
        public static VoidValue Void = new VoidValue();
        public static NullValue Null = new NullValue();
        public static UndefinedValue Undefined = new UndefinedValue();

        public HeronValue()
        {            
        }

        public virtual Object ToSystemObject()
        {
            return this;
        }

        public virtual bool ToBool()
        {
            throw new Exception("Cannot convert '" + ToString() + "' into System.Boolean");
        }

        public virtual HeronValue GetAtIndex(HeronValue index)
        {
            throw new Exception("Indexing is not supported on " + ToString());
        }

        public virtual void SetAtIndex(HeronValue index, HeronValue val)
        {
            throw new Exception("Indexing is not supported on " + ToString());
        }

        public virtual HeronValue Apply(HeronVM vm, HeronValue[] args)
        {
            throw new Exception(ToString() + " is not recognized a function object");
        }

        public virtual HeronValue GetFieldOrMethod(string name)
        {
            throw new Exception(ToString() + " does not supported GetFields()");
        }

        public virtual void SetField(string name, HeronValue val)
        {
            throw new Exception(ToString() + " does not supported SetField()");
        }

        public virtual HeronValue InvokeUnaryOperator(string s)
        {
            throw new Exception("unary operator invocation not supported on " + ToString());
        }

        public virtual HeronValue InvokeBinaryOperator(string s, HeronValue x)
        {
            switch (s)
            {
                case "==":
                    return new BoolValue(x == this);
                case "!=":
                    return new BoolValue(x != this);
                default:
                    throw new Exception("binary operator invocation not supported on " + ToString());              
            }
        }

        public abstract HeronType GetHeronType();

        public bool EqualsValue(HeronValue x)
        {
            return InvokeBinaryOperator("==", x).ToBool();
        }
    }

    public class VoidValue : HeronValue
    {
        public override string ToString()
        {
            return "Void";
        }

        public override HeronType GetHeronType()
        {
            return HeronPrimitiveTypes.VoidType;
        }
    }

    public class NullValue : HeronValue
    {
        public override string ToString()
        {
            return "null";
        }

        public override HeronType GetHeronType()
        {
            return HeronPrimitiveTypes.NullType;
        }

        public override HeronValue InvokeBinaryOperator(string s, HeronValue x)
        {
            switch (s)
            {
                case "==": 
                    return new BoolValue(x is NullValue);
                case "!=": 
                    return new BoolValue(!(x is NullValue));
                default:
                    throw new Exception("Binary operation: '" + s + "' not supported by strings");
            }
        }
    }

    public class UndefinedValue : HeronValue
    {
        public override string ToString()
        {
            return "undefined";
        }

        public override HeronType GetHeronType()
        {
            return HeronPrimitiveTypes.UndefinedType;
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

        public override HeronValue Apply(HeronVM vm, HeronValue[] args)
        {
            Object[] objs = HeronDotNet.ObjectsToDotNetArray(args);
            Object r = mi.Invoke(self, objs);
            return DotNetObject.Marshal(r);
        }

        public override HeronType GetHeronType()
        {
            return HeronPrimitiveTypes.ExternalMethodType;
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
    /// An instance of a Heron class. A HeronObject is more general in that it includes 
    /// primitive objects and .NET objects which are not part of the HeronClass 
    /// hierarchy.
    /// </summary>
    public class ClassInstance : HeronValue
    {
        public HeronClass hclass;
        public Scope fields = new Scope();
        
        public ClassInstance(HeronClass c)
        {
            hclass = c;
        }

        public override bool Equals(object obj)
        {
            return obj == this;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Creates a scope in the environment, containing variables that map to the class field names. 
        /// It is the caller's reponsibility to remove the scope. 
        /// </summary>
        /// <param name="env"></param>
        public void PushFieldsAsScope(Environment env)
        {
            env.PushScope(fields);
        }

        /// <summary>
        /// Mostly for internal purposes. 
        /// </summary>
        /// <param name="name"></param>
        public void AssureFieldDoesntExist(string name)
        {
            if (HasField(name))
                throw new Exception("field " + name + " already exists");
        }

        /// <summary>
        /// Mostly for internal purposes
        /// </summary>
        /// <param name="name"></param>
        public void AssureFieldExists(string name)
        {
            if (!HasField(name))
                throw new Exception("field " + name + " does not exist");
        }

        /// <summary>
        /// Sets the value on the named field. Does not automatically add a field if missing.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        public override void SetField(string name, HeronValue val)
        {
            if (fields.ContainsKey(name))
                fields[name] = val;
            else if (GetBase() != null)
                GetBase().SetField(name, val);
            else
                throw new Exception("Field '" + name + "' does not exist");
        }

        /// <summary>
        /// Returns true if field has already been added 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool HasField(string name)
        {
            if (fields.ContainsKey(name))
                return true;
            if (GetBase() != null)
                return GetBase().HasField(name);
            return false;
        }

        public HeronValue GetField(string name)
        {
            if (fields.ContainsKey(name))
                return fields[name];
            else if (GetBase() != null)
                return GetBase().GetField(name);
            else
                throw new Exception("Field '" + name + "' does not exist");
        }

        /// <summary>
        /// Returns true if any methods are available that have the given name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool HasMethod(string name)
        {
            return hclass.HasMethod(name);
        }

        /// <summary>
        /// Returns all functions sharing the given name at once
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public FunctionListValue GetMethods(string name)
        {
            return new FunctionListValue(this, name, hclass.GetMethods(name));
        }

        /// <summary>
        /// Adds a field. Field must not already exist. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        public void AddField(string name, HeronValue val)
        {
            AssureFieldDoesntExist(name);
            fields.Add(name, val);
        }

        public override HeronValue GetFieldOrMethod(string name)
        {
            if (fields.ContainsKey(name))
                return fields[name];
            if (HasMethod(name))
                return GetMethods(name);
            if (GetBase() != null)
                return GetBase().GetFieldOrMethod(name);
            return null;
        }

        public override string ToString()
        {
            return "{" + hclass.name + "}";
        }

        public override HeronType GetHeronType()
        {
            return hclass;
        }

        public ClassInstance GetBase()
        {
            if (!fields.ContainsKey("base"))
                return null;
            HeronValue r = fields["base"];
            if (!(r is ClassInstance))
                throw new Exception("The 'base' field should always be an instance of a class");
            return r as ClassInstance;
        }

        public HeronValue As(HeronType t)
        {
            if (t is HeronClass)
            {
                HeronClass c1 = hclass;
                HeronClass c2 = t as HeronClass;

                if (c2.name == c1.name)
                    return this;

                if (GetBase() == null)
                    throw new Exception("Could not cast from '" + hclass.name + "' to '" + t.name + "'");

                return GetBase().As(t);
            }
            else if (t is HeronInterface)
            {
                if (hclass.Implements(t as HeronInterface))
                    return new InterfaceInstance(this, t as HeronInterface);

                if (GetBase() == null)
                    throw new Exception("Could not cast from '" + hclass.name + "' to '" + t.name + "'");

                return GetBase().As(t);
            }
            throw new Exception("Could not cast from '" + hclass.name + "' to '" + t.name + "'");
        }
    }

    /// <summary>
    /// An instance of a Heron class. A HeronObject is more general in that it includes 
    /// primitive objects and .NET objects which are not part of the HeronClass 
    /// hierarchy.
    /// </summary>
    public class InterfaceInstance : HeronValue
    {   
        //  TODO: should this be an object or an instance? 
        public HeronValue obj;
        public HeronInterface hinterface;

        public InterfaceInstance(HeronValue obj, HeronInterface i)
        {
            this.obj = obj;
            hinterface = i;
        }

        public override bool Equals(object obj)
        {
            return obj == this;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Returns true if any methods are available that have the given name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool HasMethod(string name)
        {
            return hinterface.HasMethod(name);
        }

        /// <summary>
        /// Returns all functions sharing the given name at once
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public FunctionListValue GetMethods(string name)
        {
            return new FunctionListValue(obj, name, hinterface.GetMethods(name));
        }
        public override HeronValue GetFieldOrMethod(string name)
        {
            if (!HasMethod(name))
                throw new Exception("Could not find field or method '" + name + "' in '" + ToString() + "'");
            return obj.GetFieldOrMethod(name);
        }

        public override string ToString()
        {
            return "{" + hinterface.name + "}";
        }

        public override HeronType GetHeronType()
        {
            return hinterface;
        }

        public HeronValue GetObject()
        {
            return obj;
        }
   }

    /// <summary>
    /// An instance of an enumerable value.
    /// </summary>
    public class EnumInstance : HeronValue
    {
        HeronEnum henum;
        string name;

        public EnumInstance(HeronEnum e, string s)
        {
            henum = e;
            name = s;
        }

        public override HeronType GetHeronType()
        {
            return henum;
        }

        public override bool Equals(object obj)
        {
            EnumInstance that = obj as EnumInstance;
            if (that == null)
                return false;
            return that.henum.Equals(henum) && that.name == name;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode() + henum.GetHashCode();
        }
    }    
}

