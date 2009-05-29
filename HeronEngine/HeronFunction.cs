using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace HeronEngine
{
    /// <summary>
    /// Represents the definition of a Heron function or member function in the source code.
    /// Not to be confused with a FunctionObject which represents a value of function type.
    /// </summary>
    public class HeronFunction : HeronObject
    {
        public string name;
        public Statement body;
        public HeronFormalArgs formals;
        public string rettype;

        /// <summary>
        /// A function can be invoked if the 'this' value (called self) is supplied.
        /// A FunctionObject is created and then called.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="env"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public HeronObject Invoke(HeronObject self, Environment env, HeronObject[] args)
        {
            FunctionObject fo = new FunctionObject(self, this);
            return fo.Apply(env, args);
        }
    }

    /// <summary>
    /// Function names can be overloaded, so when looking up a function by name,
    /// a set of functions is returned.
    /// </summary>
    public class FunctionTable : Dictionary<string, List<HeronFunction>>
    {
        public void Add(HeronFunction f)
        {
            string s = f.name;
            if (!ContainsKey(s))
                Add(s, new List<HeronFunction>());
            this[s].Add(f);
        }

        public IEnumerable<HeronFunction> GetAllFunctions()
        {
            foreach (List<HeronFunction> list in Values)
                foreach (HeronFunction f in list)
                    yield return f;
        }
    }
}
