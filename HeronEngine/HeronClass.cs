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
        public FunctionTable methods = new FunctionTable();
        public List<Field> fields = new List<Field>();

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
            Function ctor = FindMethod("Constructor", args);
            if (ctor != null)
                ctor.Call(env, args);
            return r;
        }

        public List<Function> FindMethodGroup(string name)
        {
            if (!methods.ContainsKey(name))
                return null;
            return methods[name];
        }

        public Function FindMethod(string name, HeronObject[] args)
        {
            List<Function> methods = FindMethodGroup(name);
            if (methods == null)
                return null;
            switch (methods.Count)
            {
                case 0:
                    return null;
                case 1:
                    return methods[0];
                default:
                    // TODO: preform method resolution
                    throw new Exception("Multiple methods found mathcing " + name);
            }
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
    
