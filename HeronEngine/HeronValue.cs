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

            if (v is ExposedMethodValue)
            {
                var emv = v as ExposedMethodValue;
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
            if (t.Equals(GetHeronType()))
                return this;
            return null;
        }

        public virtual bool Is(HeronType t)
        {
            return As(t) != null;
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
    #endregion

    #region user defined type value
    /// <summary>
    /// An instance of a Heron class. A HeronObject is more general in that it includes 
    /// primitive objects and .NET objects which are not part of the ClassDefn 
    /// hierarchy.
    /// </summary>
    public class ClassInstance : HeronValue
    {
        ClassDefn cls;
        ModuleInstance module;
        Scope fields = new Scope();
        
        public ClassInstance(ClassDefn c, ModuleInstance m)
        {
            cls = c;
            module = m;
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
        public void PushFieldsAsScope(VM vm)
        {
            vm.PushScope(fields);
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
            return cls.HasMethod(name);
        }

        /// <summary>
        /// Returns all functions sharing the given name at once
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public FunDefnListValue GetMethods(string name)
        {
            return new FunDefnListValue(this, name, cls.GetMethods(name));
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
            return "{" + cls.name + "}";
        }

        public override HeronType GetHeronType()
        {
            return cls;
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
        /// Used to cast the class instance to its base class or an interface.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public override HeronValue As(HeronType t)
        {
            if (t is ClassDefn)
            {
                ClassDefn c1 = cls;
                ClassDefn c2 = t as ClassDefn;

                if (c2.name == c1.name)
                    return this;

                if (GetBase() == null)
                    return null;

                return GetBase().As(t);
            }
            else if (t is InterfaceDefn)
            {
                if (cls.Implements(t as InterfaceDefn))
                    return new InterfaceInstance(this, t as InterfaceDefn);

                if (GetBase() == null)
                    return null;

                return GetBase().As(t);
            }
            return null;
        }

        [HeronVisible]
        public string GetClassName()
        {
            return cls == null ? "_null_" : cls.GetName();
        }

        public ModuleInstance GetModuleInstance()
        {
            return module;
        }
    }

    /// <summary>
    /// An instance of a Heron class. A HeronObject is more general in that it includes 
    /// primitive objects and .NET objects which are not part of the ClassDefn 
    /// hierarchy.
    /// </summary>
    public class InterfaceInstance : HeronValue
    {   
        public ClassInstance obj;
        public InterfaceDefn hinterface;

        public InterfaceInstance(ClassInstance obj, InterfaceDefn i)
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
            if (!hinterface.HasMethod(name))
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

        public ClassInstance GetObject()
        {
            return obj;
        }

        public override HeronValue As(HeronType t)
        {
            InterfaceDefn i = t as InterfaceDefn;
            if (i == null)
                return null;
            if (hinterface.InheritsFrom(i))
                return obj;
            return null;
        }

        public ModuleInstance GetModuleInstance()
        {
            return obj.GetModuleInstance();
        }
    }

    /// <summary>
    /// An instance of an enumerable value.
    /// </summary>
    public class EnumInstance : HeronValue
    {
        EnumDefn henum;
        string name;

        public EnumInstance(EnumDefn e, string s)
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

    /// <summary>
    /// A moduleDef instance can contain fields and methods just like a class
    /// </summary>
    public class ModuleInstance : ClassInstance
    {
        Dictionary<string, ModuleInstance> importedModules = new Dictionary<string, ModuleInstance>();
 
        public ModuleInstance(ModuleDefn m, ModuleInstance i)
            : base(m, i)
        {
            if (i != null)
                throw new Exception("A module does not belong to a module");

            if (m == null)
                throw new Exception("Missing module");
        }

        public ModuleDefn GetModuleDefn()
        {
            ModuleDefn m = GetHeronType() as ModuleDefn;
            if (m == null)
                throw new Exception("Missing module");
            return m;
        }

        public ModuleInstance LookupImportedModuleInstance(string s)
        {
            if (!importedModules.ContainsKey(s))
                return null;
            return importedModules[s];
        }

        public IEnumerable<ModuleInstance> GetImportedModuleInstances()
        {
            return importedModules.Values;
        }

        public override HeronValue GetFieldOrMethod(string name)
        {
 	        HeronValue r = base.GetFieldOrMethod(name);
            if (r != null)
                return r;
            return LookupImportedModuleInstance(name);
        }
    }
    #endregion

    #region primitive values
    public abstract class PrimitiveTemplate<T> : HeronValue
    {
        T val;

        public PrimitiveTemplate(T x)
        {
            val = x;
        }

        public override string ToString()
        {
            return val.ToString();
        }

        public override object ToSystemObject()
        {
            return val;
        }

        public T GetValue()
        {
            return val;
        }

        [HeronVisible]
        public HeronValue AsString()
        {
            return new StringValue(val.ToString());
        }
    }

    public class IntValue : PrimitiveTemplate<int>
    {
        public IntValue(int x)
            : base(x)
        {
        }

        public IntValue()
            : base(0)
        {
        }

        public override HeronValue InvokeUnaryOperator(VM vm, string s)
        {
            switch (s)
            {
                case "-": return new IntValue(-GetValue());
                case "~": return new IntValue(~GetValue());
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by integers");
            }
        }

        public override HeronValue InvokeBinaryOperator(VM vm, string s, HeronValue x)
        {
            if (!(x is IntValue))
                throw new Exception("binary operation not supported on differently typed objects");

            int arg = (x as IntValue).GetValue();
            switch (s)
            {
                case "+": return new IntValue(GetValue() + arg);
                case "-": return new IntValue(GetValue() - arg);
                case "*": return new IntValue(GetValue() * arg);
                case "/": return new IntValue(GetValue() / arg);
                case "%": return new IntValue(GetValue() % arg);
                case "==": return new BoolValue(GetValue() == arg);
                case "!=": return new BoolValue(GetValue() != arg);
                case "<": return new BoolValue(GetValue() < arg);
                case ">": return new BoolValue(GetValue() > arg);
                case "<=": return new BoolValue(GetValue() <= arg);
                case ">=": return new BoolValue(GetValue() >= arg);
                case "..": return new RangeEnumerator(this, x as IntValue);
                default:
                    throw new Exception("Binary operation: '" + s + "' not supported by integers");
            }
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.IntType;
        }
    }

    public class CharValue : PrimitiveTemplate<char>
    {
        public CharValue(char x)
            : base(x)
        {
        }

        public CharValue()
            : base('\0')
        {
        }

        public override HeronValue InvokeUnaryOperator(VM vm, string s)
        {
            switch (s)
            {
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by chars");
            }
        }

        public override HeronValue InvokeBinaryOperator(VM vm, string s, HeronValue x)
        {
            char arg = (x as CharValue).GetValue();
            switch (s)
            {
                case "==": return new BoolValue(GetValue() == arg);
                case "!=": return new BoolValue(GetValue() != arg);
                default:
                    throw new Exception("Binary operation: '" + s + "' not supported by chars");
            }
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.CharType;
        }
    }

    public class FloatValue : PrimitiveTemplate<float>
    {
        public FloatValue(float x)
            : base(x)
        {
        }

        public FloatValue()
            : base(0.0f)
        {
        }

        public override HeronValue InvokeUnaryOperator(VM vm, string s)
        {
            switch (s)
            {
                case "-": return new FloatValue(-GetValue());
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by floats");
            }
        }

        public override HeronValue InvokeBinaryOperator(VM vm, string s, HeronValue x)
        {
            if (!(x is FloatValue))
                throw new Exception("binary operation not supported on differently typed objects");
            float arg = (x as FloatValue).GetValue();
            switch (s)
            {
                case "+": return new FloatValue(GetValue() + arg);
                case "-": return new FloatValue(GetValue() - arg);
                case "*": return new FloatValue(GetValue() * arg);
                case "/": return new FloatValue(GetValue() / arg);
                case "%": return new FloatValue(GetValue() % arg);
                case "==": return new BoolValue(GetValue() == arg);
                case "!=": return new BoolValue(GetValue() != arg);
                case "<": return new BoolValue(GetValue() < arg);
                case ">": return new BoolValue(GetValue() > arg);
                case "<=": return new BoolValue(GetValue() <= arg);
                case ">=": return new BoolValue(GetValue() >= arg);
                default:
                    throw new Exception("Binary operation: '" + s + "' not supported by floats");
            }
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.FloatType;
        }
    }

    public class BoolValue : PrimitiveTemplate<bool>
    {
        public BoolValue(bool x)
            : base(x)
        {
        }

        public BoolValue()
            : base(false)
        {
        }

        public override HeronValue InvokeUnaryOperator(VM vm, string s)
        {
            switch (s)
            {
                case "!": return new BoolValue(!GetValue());
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by booleans");
            }
        }

        public override HeronValue InvokeBinaryOperator(VM vm, string s, HeronValue x)
        {
            if (!(x is BoolValue))
                throw new Exception("binary operation not supported on differently typed objects");
            bool arg = (x as BoolValue).GetValue();
            switch (s)
            {
                case "==": return new BoolValue(GetValue() == arg);
                case "!=": return new BoolValue(GetValue() != arg);
                case "&&": return new BoolValue(GetValue() && arg);
                case "||": return new BoolValue(GetValue() || arg);
                case "^^": return new BoolValue(GetValue() ^ arg);
                default:
                    throw new Exception("Binary operation: '" + s + "' not supported by booleans");
            }
        }

        public override bool ToBool()
        {
            return GetValue();
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.BoolType;
        }

        public override string ToString()
        {
            return GetValue() ? "true" : "false";
        }
    }

    public class StringValue : PrimitiveTemplate<string>
    {
        public StringValue(string x)
            : base(x)
        {
        }

        public StringValue()
            : base("")
        {
        }

        public override HeronValue InvokeUnaryOperator(VM vm, string s)
        {
            switch (s)
            {
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by strings");
            }
        }

        public override HeronValue InvokeBinaryOperator(VM vm, string s, HeronValue x)
        {
            if (!(x is StringValue))
                throw new Exception("binary operation not supported on differently typed objects");
            StringValue so = x as StringValue;
            string arg = (x as StringValue).GetValue();
            switch (s)
            {
                case "+": return new StringValue(GetValue() + arg);
                case "==": return new BoolValue(GetValue() == so.GetValue());
                case "!=": return new BoolValue(GetValue() != so.GetValue());
                default:
                    throw new Exception("Binary operation: '" + s + "' not supported by strings");
            }
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.StringType;
        }

        [HeronVisible]
        public HeronValue Length()
        {
            return new IntValue(GetValue().Length);
        }

        [HeronVisible]
        public HeronValue GetChar(IntValue index)
        {
            return new CharValue(GetValue()[index.GetValue()]);
        }
    }
    #endregion
}

