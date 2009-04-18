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
        public string type;
    }

    /// <summary>
    /// Represents the formal argument to a function
    /// </summary>
    public class FormalArg
    {
        public string name;
        public string type;
    }

    /// <summary>
    /// Represents all of the formals arguments to a function.
    /// </summary>
    public class FormalArgs : List<FormalArg>
    {
    }

    /// <summary>
    /// A class derives from HObject because it can be treated like a value 
    /// in Heron.
    /// </summary>
    public class HeronClass : HeronType
    {
        public string name;
        FunctionTable methods = new FunctionTable();
        List<Field> fields = new List<Field>();

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
            List<Function> ctorlist = GetMethods("Constructor");
            if (ctorlist == null)
                return r;
            FunctionListObject ctors = new FunctionListObject(r, "Constructor", ctorlist);
            FunctionObject o = ctors.Resolve(args);
            if (o == null)
                return r; // No matching constructor
            o.Apply(env, args);                
            return r;
        }

        public FunctionListObject GetCtors()
        {
            return new FunctionListObject(null, "Constructor", GetMethods("Constructor"));
        }

        public List<Function> GetMethods(string name)
        {
            if (!HasMethod(name))
                return null;
            return methods[name];
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
    }

    /// <summary>
    /// Represents a list of classes.
    /// </summary>
    public class Module
    {
        public string name;
        public List<HeronClass> classes = new List<HeronClass>();

        public HeronClass GetMainClass()
        {
            foreach (HeronClass c in classes)
                if (c.name == "Main")
                    return c;
            return null;
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
    
