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
    /// An attribute used to identify methods and properties on a HeronValue derived type 
    /// which are to be exposed automatically to Heron. The exposed functions are managed by PrimitiveType.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property)]    
    public class HeronVisible : Attribute
    {
    }

    /// <summary>
    /// This is the base class of all values that are exposed to Heron.
    /// </summary>
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

        /// <summary>
        /// Treats this value as a function and calls it.
        /// </summary>
        /// <param name="vm"></param>
        /// <param name="args"></param>
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
            HeronType t = GetHeronType();
            ExposedMethod m = t.GetMethod(name); 
            if (m != null)
                return m.CreateBoundMethod(this);
            FieldDefn f = t.GetField(name);
            if (f != null)
                return f.GetValue(this);
            throw new Exception("Could not find field or method : " + name);
        }

        /// <summary>
        /// Sets the value associated with the named field
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        public virtual void SetField(string name, HeronValue val)
        {
            HeronType t = GetHeronType();
            FieldDefn fi = t.GetField(name);
            fi.SetValue(this, val);
        }

        public virtual HeronValue InvokeUnaryOperator(VM vm, string s)
        {
            throw new Exception("unary operator invocation not supported on " + ToString());
        }

        public virtual HeronValue InvokeBinaryOperator(VM vm, string s, HeronValue x)
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

        public virtual bool EqualsValue(VM vm, HeronValue x)
        {
            return InvokeBinaryOperator(vm, "==", x).ToBool();
        }

        [HeronVisible]
        public override string ToString()
        {
            return base.ToString();
        }
    }

    /// <summary>
    /// Used to represent void types, which are non-value returned from a function.
    /// </summary>
    public class VoidValue : HeronValue
    {
        public override string ToString()
        {
            return "Void";
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.VoidType;
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

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.NullType;
        }

        public override HeronValue InvokeBinaryOperator(VM vm, string s, HeronValue x)
        {
            switch (s)
            {
                case "==": 
                    return new BoolValue(x is NullValue);
                case "!=": 
                    return new BoolValue(!(x is NullValue));
                default:
                    throw new Exception("Binary operation: '" + s + "' not supported by null");
            }
        }
    }

    /// <summary>
    /// Not currently used 
    /// </summary>
    public class UndefinedValue : HeronValue
    {
        public override string ToString()
        {
            return "undefined";
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.UndefinedType;
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

        /// <summary>
        /// Returns the field associated with the name. Throws 
        /// an exception if it does not exist.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
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
        public FunDefnListValue GetMethods(string name)
        {
            return new FunDefnListValue(this, name, hclass.GetMethods(name));
        }

        /// <summary>
        /// Adds a field. FieldDefn must not already exist. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        public void AddField(string name, HeronValue val)
        {
            AssureFieldDoesntExist(name);
            fields.Add(name, val);
        }

        /// <summary>
        /// Gets a field or method associated with the name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Returns the base class that that this class instance derives from,
        /// or NULL if not applicable.
        /// </summary>
        /// <returns></returns>
        public ClassInstance GetBase()
        {
            if (!fields.ContainsKey("base"))
                return null;
            HeronValue r = fields["base"];
            if (!(r is ClassInstance))
                throw new Exception("The 'base' field should always be an instance of a class");
            return r as ClassInstance;
        }

        /// <summary>
        /// Used to cast the class instance to its base class.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
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

        [HeronVisible]
        HeronValue GetTypeName()
        {
            return hclass.GetName();
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
        public FunDefnListValue GetMethods(string name)
        {
            return new FunDefnListValue(obj, name, hinterface.GetMethods(name));
        }
        public override HeronValue GetFieldOrMethod(string name)
        {
            if (!HasMethod(name))
                base.GetFieldOrMethod(name);
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

