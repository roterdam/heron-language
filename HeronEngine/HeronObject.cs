using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace HeronEngine
{
    public class HeronObject
    {
        string type = "undefined";

        public static HeronObject Void = new HeronObject("void");
        public static HeronObject Null = new HeronObject("null");

        public HeronObject()
        {            
        }

        public HeronObject(string type)
        {
            this.type = type;
        }

        public string GetHeronType()
        {
            return type;
        }

        public virtual Object ToSystemObject()
        {
            throw new Exception("Cannot convert " + type + " into System.Object");
        }

        public virtual bool ToBool()
        {
            throw new Exception("Cannot convert " + type + " into System.Boolean");
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

        public virtual HeronObject GetField(string name)
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
        
        static public Object[] ObjectsToDotNetArray(HeronObject[] array)
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
            Object[] objs = HeronObject.ObjectsToDotNetArray(args);
            Object r = mi.Invoke(self, objs);
            return DotNetObject.Marshal(r);
        }
    }

    public class DotNetObject : HeronObject
    {
        Object obj;

        /// <summary>
        /// This is private because you should used DotNetObject.Marshal instead
        /// </summary>
        /// <param name="obj"></param>
        private DotNetObject(Object obj)
        {
            this.obj = obj;
        }

        /// <summary>
        /// Creates a Heron object from a System (.NET) object
        /// If it is a primitive, this will convert to the Heron primitives
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static HeronObject Marshal(Object o)
        {
            if (o is Double)
            {
                return new FloatObject((double)o);
            }
            else if (o is Int32)
            {
                return new IntObject((int)o);
            }
            else if (o is Char)
            {
                return new CharObject((char)o);
            }
            else if (o is String)
            {
                return new StringObject((string)o);
            }
            else if (o is Boolean)
            {
                return new BoolObject((bool)o);
            }
            else 
            {
                return new DotNetObject(o);
            }
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

        public override HeronObject GetField(string name)
        {
            Type type = GetSystemType();

            // We have to first look to see if there are static fields
            FieldInfo[] fis = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetField);
            foreach (FieldInfo fi in fis)
                if (fi.Name == name)
                    return DotNetObject.Marshal(fi.GetValue(null));

            // Look for methods
            MethodInfo[] mis = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod);
            if (mis.Length != 0)
                return new DotNetMethodGroup(this, name);

            // No static field or method found.
            // TODO: could eventually support property.
            throw new Exception("Could not find field, or static method " + name);
        }            
    }

    public class PrimitiveObject<T> : HeronObject 
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
    }

    public class FloatObject : PrimitiveObject<double>
    {
        public FloatObject(double x)
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
            double arg = (x as FloatObject).GetValue();
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
            string arg = (x as StringObject).GetValue();
            switch (s)
            {
                case "+": return new StringObject(GetValue() + arg);
                default:
                    throw new Exception("Binary operation: '" + s + "' not supported by strings");
            }
        }
    }

    public class ListObject : HeronObject, IEnumerable<HeronObject>
    {
        List<HeronObject> list = new List<HeronObject>();


        #region IEnumerable<HeronObject> Members

        public IEnumerator<HeronObject> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        #endregion
    }

    /// <summary>
    /// An instance of a Heron class. A HeronObject is more general in that it includes 
    /// primitive objects and .NET objects which are not part of the HeronClass 
    /// hierarchy.
    /// </summary>
    public class Instance : HeronObject
    {
        public HeronClass hclass;
        public ObjectTable fields = new ObjectTable();

        public Instance(HeronClass c)
        {
            hclass = c;
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
            AssureFieldExists(name);
            fields[name] = val;
        }

        /// <summary>
        /// Returns true if field has already been added 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool HasField(string name)
        {
            return fields.ContainsKey(name);
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

        /// <summary>
        /// Adds a field if it does not exist, otherwise simple sets the value. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        public void AddOrSetFieldValue(string name, HeronObject val)
        {
            if (HasField(name))
                fields[name] = val;
            fields.Add(name, val);
        }

        public override HeronObject GetField(string name)
        {
            if (HasField(name))
                return fields[name];
            if (HasMethod(name))
                return GetMethods(name);
            throw new Exception("Could not find field or method " + name);
        }

        public override string ToString()
        {
            string r = hclass.name;
            r += " = { ";
            foreach (string key in fields.Keys)
            {
                string val = fields[key].ToString();
                r += key + " = " + val + "; ";
            }
            r += " }";
            return r;
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
        Function fun;

        public FunctionObject(HeronObject self, Function f)
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

        public Instance GetSelfAsInstance()
        {
            return self as Instance;
        }

        public override HeronObject Apply(Environment env, HeronObject[] args)
        {
            // Create a stack frame 
            env.PushNewFrame(fun, GetSelfAsInstance());

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
        List<Function> functions = new List<Function>();

        public FunctionListObject(HeronObject self, string name, IEnumerable<Function> args)
        {
            this.self = self;
            foreach (Function f in args)
                functions.Add(f);
            this.name = name;
            foreach (Function f in functions)
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
            FunctionObject r = null;
            foreach (Function f in functions)
            {
                if (f.formals.Count == args.Length)
                {
                    if (r != null)
                        throw new Exception("Ambiguous function resolution of " + name);
                    r = new FunctionObject(self, f);
                }
            }
            if (r == null)
                throw new Exception("Unable to resolve function " + name);
            return r;
        }

        public override HeronObject Apply(Environment env, HeronObject[] args)
        {
            Trace.Assert(functions.Count > 0);
            return Resolve(args).Apply(env, args);
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
            Object[] os = HeronObject.ObjectsToDotNetArray(args);
            Object o = self.GetSystemType().InvokeMember(name, BindingFlags.InvokeMethod, null, self.ToSystemObject(), os);
            return DotNetObject.Marshal(o);
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
            Object[] os = HeronObject.ObjectsToDotNetArray(args);
            Object o = self.GetSystemType().InvokeMember(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, os);
            return DotNetObject.Marshal(o); 
        }
    }
}

