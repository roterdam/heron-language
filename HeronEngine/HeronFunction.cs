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
    public class Function : HeronObject
    {
        public string name;
        public HeronClass hclass;
        public Statement body;
        public FormalArgs formals;
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
    public class FunctionTable : Dictionary<string, List<Function>>
    {
        public void Add(Function f)
        {
            string s = f.name;
            if (!ContainsKey(s))
                Add(s, new List<Function>());
            this[s].Add(f);
        }

        public IEnumerable<Function> GetAllFunctions()
        {
            foreach (List<Function> list in Values)
                foreach (Function f in list)
                    yield return f;
        }
    }
}
