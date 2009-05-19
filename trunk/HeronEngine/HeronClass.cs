using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace HeronEngine
{
    /// <summary>
    /// Represents a member field of a class. 
    /// </summary>
    public class Field
    {
        public string name;
        public string type = "Object";
    }

    /// <summary>
    /// Represents the formal argument to a function
    /// </summary>
    public class FormalArg
    {
        public string name;
        public string type = "Object";
    }

    /// <summary>
    /// Represents all of the formals arguments to a function.
    /// </summary>
    public class FormalArgs : List<FormalArg>
    {
    }

    public class HeronClass : HeronType
    {
        public string name;
        FunctionTable methods = new FunctionTable();
        List<Field> fields = new List<Field>();
        FunctionListObject ctors;

        public HeronClass()
        {
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="env"></param>
        /// <returns></returns>
        public override HeronObject Instantiate(Environment env, HeronObject[] args)
        {
            Instance r = new Instance(this);
            foreach (Field field in fields)
                r.AddField(field.name, null);
            List<Function> ctorlist = new List<Function>(GetMethods("Constructor"));
            if (ctorlist == null)
                return r;
            ctors = new FunctionListObject(r, "Constructor", ctorlist);
            if (ctors.Count == 0)
                return r;
            FunctionObject o = ctors.Resolve(args);
            if (o == null)
                return r; // No matching constructor
            o.Apply(env, args);                
            return r;
        }

        public FunctionListObject GetCtors()
        {
            return ctors;
        }

        public IEnumerable<Function> GetMethods(string name)
        {
            if (!HasMethod(name))
                return new List<Function>();
            return methods[name];
        }

        public IEnumerable<Function> GetMethods()
        {
            return methods.GetAllFunctions();
        }

        public IEnumerable<Field> GetFields()
        {
            return fields;
        }

        public bool HasMethod(string name)
        {
            return methods.ContainsKey(name);
        }

        public void AddMethod(Function x)
        {
            methods.Add(x);
        }

        public void AddField(Field x)
        {
            fields.Add(x);
        }

        public Field GetField(string s)
        {
            foreach (Field f in fields)
                if (f.name == s)
                    return f;
            return null;
        }

        public FunctionTable GetMethodTable()
        {
            return methods;
        }
    }

    /// <summary>
    /// An interface is essentially a class without attributes or constructors.
    /// </summary>
    public class HeronInterface : HeronType
    {
        public string name;
        FunctionTable methods = new FunctionTable();

        public override HeronObject Instantiate(Environment env, HeronObject[] args)
        {
            throw new Exception("Cannot instantiate an interface");
        }

        public IEnumerable<Function> GetMethods(string name)
        {
            if (!HasMethod(name))
                return new List<Function>();
            return methods[name];
        }

        public IEnumerable<Function> GetMethods()
        {
            return methods.GetAllFunctions();
        }

        public bool HasMethod(string name)
        {
            return methods.ContainsKey(name);
        }

        public void AddMethod(Function x)
        {
            methods.Add(x);
        }

        public FunctionTable GetMethodTable()
        {
            return methods;
        }
    }

    /// <summary>
    /// Represents a list of classes.
    /// </summary>
    public class Module
    {
        public string name;
        public List<HeronClass> classes = new List<HeronClass>();
        public List<HeronInterface> interfaces = new List<HeronInterface>();

        public HeronClass GetMainClass()
        {
            return FindClass("Main");
        }

        public HeronClass GetPremainClass()
        {
            return FindClass("Precompile");
        }

        HeronClass FindClass(string s)
        {
            foreach (HeronClass c in classes)
                if (c.name == s)
                    return c;
            return null;
        }

        HeronInterface FindInterface(string s)
        {
            foreach (HeronInterface i in interfaces)
                if (i.name == s)
                    return i;
            return null;
        }

        public bool ContainsClass(string s)
        {
            return FindClass(s) != null;
        }

        public bool ContainsInterface(string s)
        {
            return FindInterface(s) != null;
        }
    }

    /// <summary>
    /// Represents a simple list of programs.
    /// </summary>
    public class HeronProgram
    {
        public List<Module> modules = new List<Module>();
    }
}
    
