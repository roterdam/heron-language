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
    /// Represents the formal argument to a function or member. 
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
    public class Class : HObject
    {
        public string name;
        public FunctionTable methods = new FunctionTable();
        public List<Field> fields = new List<Field>();

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="env"></param>
        /// <returns></returns>
        public Instance Instantiate(Environment env)
        {
            Instance r = new Instance(this);
            foreach (Field f in fields)
                r.AddField(f.name, null);
            return r;
        }

        /// TODO: figure out what the story is going to be for classes 
        /// that represent built-in types. 
    }

    public class HeronModule
    {
        public List<Class> classes = new List<Class>();
    }

    public class HeronProgram
    {
        public List<HeronModule> modules = new List<HeronModule>();
    }
}
    
