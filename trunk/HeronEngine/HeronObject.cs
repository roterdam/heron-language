using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace HeronEngine
{
    public abstract class HeronObject
    {
        public static VoidObject Void = new VoidObject();
        public static NullObject Null = new NullObject();
        public static UndefinedObject Undefined = new UndefinedObject();

        public HeronObject()
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

        public virtual HeronObject GetAt(HeronObject index)
        {
            throw new Exception("unimplemented");
        }

        public virtual void SetAt(HeronObject index, HeronObject val)
        {
            throw new Exception("unimplemented");
        }

        public virtual HeronObject Apply(Environment env, HeronObject[] args)
        {
            throw new Exception(ToString() + " is not recognized a function object");
        }

        public virtual HeronObject GetFieldOrMethod(string name)
        {
            throw new Exception(ToString() + " does not supported GetFields()");
        }

        public virtual void SetField(string name, HeronObject val)
        {
            throw new Exception(ToString() + " does not supported SetField()");
        }

        public virtual HeronObject InvokeUnaryOperator(string s)
        {
            throw new Exception("unary operator invocation not supported on " + ToString());
        }

        public virtual HeronObject InvokeBinaryOperator(string s, HeronObject x)
        {
            throw new Exception("binary operator invocation not supported on " + ToString());
                
        }

        public abstract HeronType GetHeronType();
    }

    public class VoidObject : HeronObject
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

    public class NullObject : HeronObject
    {
        public override string ToString()
        {
            return "null";
        }

        public override HeronType GetHeronType()
        {
            return HeronPrimitiveTypes.NullType;
        }

        public override HeronObject InvokeBinaryOperator(string s, HeronObject x)
        {
            switch (s)
            {
                case "==": return new BoolObject(x is NullObject);
                case "!=": return new BoolObject(!(x is NullObject));
                default:
                    throw new Exception("Binary operation: '" + s + "' not supported by strings");
            }
        }

    }

    public class UndefinedObject : HeronObject
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

    public class DotNetMethod : HeronObject
    {
        MethodInfo mi;
        HeronObject self;

        public DotNetMethod(MethodInfo mi, HeronObject self)
        {
            this.mi = mi;
            this.self = self;
        }

        public override HeronObject Apply(Environment env, HeronObject[] args)
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

    public class DotNetObject : HeronObject
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
        public static HeronObject Marshal(Object o)
        {
            return HeronDotNet.DotNetToHeronObject(o);
        }

        internal static HeronObject CreateDotNetObjectNoMarshal(Object o)
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

        public override HeronObject GetFieldOrMethod(string name)
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

    public abstract class PrimitiveObject<T> : HeronObject 
    {
        T val;

        public PrimitiveObject(T x)
        {
            val = x;
        }

        public PrimitiveObject()
        {
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
    }

    public class IntObject : PrimitiveObject<int>
    {
        public IntObject(int x)
            : base(x)
        {
        }

        public IntObject()
        {
        }

        public override HeronObject InvokeUnaryOperator(string s)
        {
            switch (s)
            {
                case "-": return new IntObject(-GetValue());
                case "~": return new IntObject(~GetValue());
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by integers");
            }
        }

        public override HeronObject InvokeBinaryOperator(string s, HeronObject x)
        {
            if (!(x is IntObject))
                throw new Exception("binary operation not supported on differently typed objects");

            int arg = (x as IntObject).GetValue();
            switch (s)
            {
                case "+": return new IntObject(GetValue() + arg);
                case "-": return new IntObject(GetValue() - arg);
                case "*": return new IntObject(GetValue() * arg);
                case "/": return new IntObject(GetValue() / arg);
                case "%": return new IntObject(GetValue() % arg);
                case "==": return new BoolObject(GetValue() == arg);
                case "!=": return new BoolObject(GetValue() != arg);
                case "<": return new BoolObject(GetValue() < arg);
                case ">": return new BoolObject(GetValue() > arg);
                case "<=": return new BoolObject(GetValue() <= arg);
                case ">=": return new BoolObject(GetValue() >= arg);
                default:
                    throw new Exception("Binary operation: '" + s + "' not supported by integers");
            }
        }

        public override HeronType GetHeronType()
        {
            return HeronPrimitiveTypes.IntType;
        }
    }

    public class CharObject : PrimitiveObject<char>
    {
        public CharObject(char x)
            : base(x)
        {
        }

        public CharObject()
        {
        }

        public override HeronObject InvokeUnaryOperator(string s)
        {
            switch (s)
            {
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by chars");
            }
        }

        public override HeronObject InvokeBinaryOperator(string s, HeronObject x)
        {
            switch (s)
            {
                default:
                    throw new Exception("Binary operation: '" + s + "' not supported by chars");
            }
        }

        public override HeronType GetHeronType()
        {
            return HeronPrimitiveTypes.CharType;
        }
    }

    public class FloatObject : PrimitiveObject<float>
    {
        public FloatObject(float x)
            : base(x)
        {
        }

        public FloatObject()
        {
        }

        public override HeronObject InvokeUnaryOperator(string s)
        {
            switch (s)
            {
                case "-": return new FloatObject(-GetValue());
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by integers");
            }
        }

        public override HeronObject InvokeBinaryOperator(string s, HeronObject x) 
        {
            if (!(x is FloatObject))
                throw new Exception("binary operation not supported on differently typed objects");
            float arg = (x as FloatObject).GetValue();
            switch (s)
            {
                case "+": return new FloatObject(GetValue() + arg);
                case "-": return new FloatObject(GetValue() - arg);
                case "*": return new FloatObject(GetValue() * arg);
                case "/": return new FloatObject(GetValue() / arg);
                case "%": return new FloatObject(GetValue() % arg);
                case "==": return new BoolObject(GetValue() == arg);
                case "!=": return new BoolObject(GetValue() != arg);
                case "<": return new BoolObject(GetValue() < arg);
                case ">": return new BoolObject(GetValue() > arg);
                case "<=": return new BoolObject(GetValue() <= arg);
                case ">=": return new BoolObject(GetValue() >= arg);
                default:
                    throw new Exception("Binary operation: '" + s + "' not supported by floats");
            }
        }

        public override HeronType GetHeronType()
        {
            return HeronPrimitiveTypes.FloatType;
        }
    }

    public class BoolObject : PrimitiveObject<bool>
    {
        public BoolObject(bool x)
            : base(x)
        {
        }

        public BoolObject()
        {
        }

        public override HeronObject InvokeUnaryOperator(string s)
        {
            switch (s)
            {
                case "!": return new BoolObject(!GetValue());
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by booleans");
            }
        }

        public override HeronObject InvokeBinaryOperator(string s, HeronObject x)
        {
            if (!(x is BoolObject))
                throw new Exception("binary operation not supported on differently typed objects");
            bool arg = (x as BoolObject).GetValue();
            switch (s)
            {
                case "==": return new BoolObject(GetValue() == arg);
                case "!=": return new BoolObject(GetValue() != arg);
                case "&&": return new BoolObject(GetValue() && arg);
                case "||": return new BoolObject(GetValue() || arg);
                case "^^": return new BoolObject(GetValue() ^ arg);
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
            return HeronPrimitiveTypes.BoolType;
        }
    }

    public class StringObject : PrimitiveObject<string>
    {
        public StringObject(string x)
            : base(x)
        {
        }

        public StringObject()
        {
        }

        public override HeronObject InvokeUnaryOperator(string s)
        {
            switch (s)
            {
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by integers");
            }
        }

        public override HeronObject InvokeBinaryOperator(string s, HeronObject x)
        {
            if (!(x is StringObject))
                throw new Exception("binary operation not supported on differently typed objects");
            StringObject so = x as StringObject;
            string arg = (x as StringObject).GetValue();
            switch (s)
            {
                case "+": return new StringObject(GetValue() + arg);
                case "==": return new BoolObject(GetValue() == so.GetValue());
                case "!=": return new BoolObject(GetValue() != so.GetValue());
                default:
                    throw new Exception("Binary operation: '" + s + "' not supported by strings");
            }
        }

        public override HeronType GetHeronType()
        {
            return HeronPrimitiveTypes.StringType;
        }
    }

    /// <summary>
    /// An instance of a Heron class. A HeronObject is more general in that it includes 
    /// primitive objects and .NET objects which are not part of the HeronClass 
    /// hierarchy.
    /// </summary>
    public class ClassInstance : HeronObject
    {
        public HeronClass hclass;
        public ObjectTable fields = new ObjectTable();
        
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
        public override void SetField(string name, HeronObject val)
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

        public HeronObject GetField(string name)
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
        public FunctionListObject GetMethods(string name)
        {
            return new FunctionListObject(this, name, hclass.GetMethods(name));
        }

        /// <summary>
        /// Adds a field. Field must not already exist. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        public void AddField(string name, HeronObject val)
        {
            AssureFieldDoesntExist(name);
            fields.Add(name, val);
        }

        public override HeronObject GetFieldOrMethod(string name)
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
            HeronObject r = fields["base"];
            if (!(r is ClassInstance))
                throw new Exception("The 'base' field should always be an instance of a class");
            return r as ClassInstance;
        }

        public HeronObject As(HeronType t)
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
    public class InterfaceInstance : HeronObject
    {
        //  TODO: should this be an object or an instance? 
        public HeronObject obj;
        public HeronInterface hinterface;

        public InterfaceInstance(HeronObject obj, HeronInterface i)
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
        public FunctionListObject GetMethods(string name)
        {
            return new FunctionListObject(obj, name, hinterface.GetMethods(name));
        }
        public override HeronObject GetFieldOrMethod(string name)
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

        public HeronObject GetObject()
        {
            return obj;
        }
   }

    public class EnumInstance : HeronObject
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
    
    /// <summary>
    /// Represents an object that can be applied to arguments (i.e. called)
    /// You can think of this as a member function bound to the this argument.
    /// In C# this would be called a delegate.
    /// </summary>
    public class FunctionObject : HeronObject
    {
        HeronObject self;
        HeronFunction fun;

        public FunctionObject(HeronObject self, HeronFunction f)
        {
            this.self = self;
            fun = f;
        }

        private void PushArgsAsScope(Environment env, HeronObject[] args)
        {
            env.PushScope();
            int n = fun.formals.Count;
            Trace.Assert(n == args.Length);
            for (int i = 0; i < n; ++i)
                env.AddVar(fun.formals[i].name, args[i]);
        }

        public ClassInstance GetSelfAsInstance()
        {
            return self as ClassInstance;
        }

        public void PerformConversions(HeronObject[] xs)
        {
            for (int i = 0; i < xs.Length; ++i)
            {
                Any a = new Any(xs[i]);
                xs[i] = a.As(GetFormalType(i));
            }
        }

        public HeronType GetFormalType(int n)
        {
            return fun.formals[n].type;
        }

        public override HeronObject Apply(Environment env, HeronObject[] args)
        {
            // Create a stack frame 
            env.PushNewFrame(fun, GetSelfAsInstance());

            // Convert the arguments into appropriate types
            // TODO: optimize
            PerformConversions(args);

            // Create a new scope containing the arguments 
            PushArgsAsScope(env, args);

            // Execute the function body
            fun.body.Execute(env);

            // Pop the arguments scope
            env.PopScope();

            // Pop the calling frame
            env.PopFrame();

            // Gets last result and resets it
            return env.GetLastResult();
        }

        public override HeronType GetHeronType()
        {
            return HeronPrimitiveTypes.FunctionType;
        }
    }

    /// <summary>
    /// Represents a group of similiar functions. 
    /// In most cases these would all have the same name. 
    /// This is class is used for dynamic resolution of overloaded function
    /// names.
    /// </summary>
    public class FunctionListObject : HeronObject
    {
        HeronObject self;
        string name;
        List<HeronFunction> functions = new List<HeronFunction>();

        public int Count
        {
            get
            {
                return functions.Count;
            }
        }

        public FunctionListObject(HeronObject self, string name, IEnumerable<HeronFunction> args)
        {
            this.self = self;
            foreach (HeronFunction f in args)
                functions.Add(f);
            this.name = name;
            foreach (HeronFunction f in functions)
                if (f.name != name)
                    throw new Exception("All functions in function list object must share the same name");
        }

        /// <summary>
        /// This is a very primitive resolution function that only looks at the number of arguments 
        /// provided. A more sophisticated function would look at the types.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public FunctionObject Resolve(HeronObject[] args)
        {
            List<FunctionObject> r = new List<FunctionObject>();
            foreach (HeronFunction f in functions)
            {
                if (f.formals.Count == args.Length)
                {
                    r.Add(new FunctionObject(self, f));
                }
            }
            if (r.Count == 0)
                return null;
            else if (r.Count == 1)
                return r[0];
            else
                return FindBestMatch(r, args);
        }

        public FunctionObject FindBestMatch(List<FunctionObject> list, HeronObject[] args)
        {
            // Each iteration removes candidates. This list holds all of the matches
            // Necessary, because removing items from a list while we iterate it is hard.
            List<FunctionObject> tmp = new List<FunctionObject>(list);
            for (int pos=0; pos < args.Length; ++pos)
            {
                // On each iteration, update the main list, to only contain the remaining items
                list = new List<FunctionObject>(tmp);
                HeronType argType = args[pos].GetHeronType();
                for (int i=0; i < list.Count; ++i)
                {
                    FunctionObject fo = list[i];
                    HeronType formalType = fo.GetFormalType(pos);
                    if (!formalType.Equals(argType))
                        tmp.Remove(fo);                        
                }
                if (tmp.Count == 0)
                    throw new Exception("Could not resolve function, no function matches perfectly");
                
                // We found a single best match
                if (tmp.Count == 1)
                    return tmp[0];
            }

            Trace.Assert(tmp.Count > 1);
            throw new Exception("Could not resolve function, several matched perfectly");
        }

        public override HeronObject Apply(Environment env, HeronObject[] args)
        {
            Trace.Assert(functions.Count > 0);
            FunctionObject o = Resolve(args);
            if (o == null)
                throw new Exception("Could not resolve function '" + name + "' with arguments " + ArgsToString(args));
            return o.Apply(env, args);
        }

        public string ArgsToString(HeronObject[] args)
        {
            string r = "(";
            for (int i = 0; i < args.Length; ++i)
            {
                if (i > 0) r += ", ";
                r += args[i].ToString();
            }
            r += ")";
            return r;
        }

        public override HeronType GetHeronType()
        {
            return HeronPrimitiveTypes.FunctionListType;
        }
    }

    /// <summary>
    /// In .NET methods are overloaded, so resolve a method name on a .NET object
    /// yields a group of methods. This class stores the object and the method
    /// name for invocation.
    /// </summary>
    public class DotNetMethodGroup : HeronObject
    {
        DotNetObject self;
        string name;

        public DotNetMethodGroup(DotNetObject self, string name)
        { 
            this.self = self;
            this.name = name;
        }

        public override HeronObject Apply(Environment env, HeronObject[] args)
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
    public class DotNetStaticMethodGroup : HeronObject
    {
        DotNetClass self;
        string name;
         
        public DotNetStaticMethodGroup(DotNetClass self, string name)
        {
            this.self = self;
            this.name = name;            
        }

        public override HeronObject Apply(Environment env, HeronObject[] args)
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

