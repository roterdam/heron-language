using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace HeronEngine
{
    /// <summary>
    /// Represents the definition of a Heron function or member function
    /// </summary>
    public class Function : HeronObject
    {
        public string name;
        public HeronClass hclass;
        public Statement body;
        public FormalArgs formals;
        public string rettype;
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
    }
}
