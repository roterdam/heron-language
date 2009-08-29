using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace HeronEngine
{
    /// <summary>
    /// Represents the formal argument to a function
    /// </summary>
    public class HeronFormalArg
    {
        public string name;
        public HeronType type = HeronPrimitiveTypes.AnyType;
    }

    /// <summary>
    /// Represents all of the formals arguments to a function.
    /// </summary>
    public class HeronFormalArgs : List<HeronFormalArg>
    {
    }

    /// <summary>
    /// Represents the definition of a Heron function or member function in the source code.
    /// Not to be confused with a FunctionObject which represents a value of function type.
    /// </summary>
    public class HeronFunction : HeronObject
    {
        public string name;
        public Statement body;
        public HeronFormalArgs formals;
        public HeronType parent;
        public HeronType rettype;

        public HeronFunction(HeronType parent)
        {
            this.parent = parent;
        }

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

        public bool Matches(HeronFunction f)
        {
            if (f.name != name)
                return false;
            int n = formals.Count;
            if (f.formals.Count != n)
                return false;
            for (int i = 0; i < n; ++i)
            {
                HeronFormalArg arg1 = formals[i];
                HeronFormalArg arg2 = f.formals[i];
                if (arg1.type != arg2.type)
                    return false;
            }
            return true;
        }

        public override HeronType GetHeronType()
        {
            return HeronPrimitiveTypes.FunctionType;
        }

        public HeronType GetParentType()
        {
            return parent;
        }

        internal void ResolveTypes()
        {
            // Resolve the return type
            if (rettype is UnresolvedType)
                rettype = (rettype as UnresolvedType).Resolve();

            // Resolve the argument types
            foreach (HeronFormalArg arg in formals)
                if (arg.type is UnresolvedType)
                    arg.type = (arg.type as UnresolvedType).Resolve();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(name);
            sb.Append("(");
            for (int i = 0; i < formals.Count; ++i)
            {
                if (i > 0)
                    sb.Append(", ");
                HeronFormalArg arg = formals[i];
                sb.Append(arg.name).Append(" : ").Append(arg.type.ToString());
            }
            sb.Append(")");
            if (rettype != null)
                sb.Append(" : ").Append(rettype.ToString());

            return sb.ToString();
        }
    }

    /// <summary>
    /// </summary>
    public class FunctionTable : List<HeronFunction>
    {
    }
}
