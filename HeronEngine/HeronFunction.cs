/// Heron language interpreter for Windows in C#
/// http://www.heron-language.com
/// Copyright (c) 2009 Christopher Diggins
/// Licenced under the MIT License 1.0 
/// http://www.opensource.org/licenses/mit-license.php

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
    public class FormalArg
    {
        public string name;
        public HeronType type = PrimitiveTypes.AnyType;
    }

    /// <summary>
    /// Represents all of the formals arguments to a function.
    /// </summary>
    public class HeronFormalArgs : List<FormalArg>
    {
    }

    /// <summary>
    /// Represents the definition of a Heron function or member function in the source code.
    /// Not to be confused with a FunctionVale which represents a value of function type.
    /// </summary>
    public class FunctionDefinition : HeronValue
    {
        public string name = "_anonymous_";
        public Statement body;
        public HeronFormalArgs formals;
        public HeronType parent;
        public HeronType rettype;

        public FunctionDefinition(HeronType parent)
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
        public HeronValue Invoke(HeronValue self, VM vm, HeronValue[] args)
        {
            // TODO: in theory we can optimize this
            FunctionValue fo = new FunctionValue(self, this);
            return fo.Apply(vm, args);
        }

        public bool Matches(FunctionDefinition f)
        {
            if (f.name != name)
                return false;
            int n = formals.Count;
            if (f.formals.Count != n)
                return false;
            for (int i = 0; i < n; ++i)
            {
                FormalArg arg1 = formals[i];
                FormalArg arg2 = f.formals[i];
                if (arg1.type != arg2.type)
                    return false;
            }
            return true;
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.FunctionType;
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
            foreach (FormalArg arg in formals)
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
                FormalArg arg = formals[i];
                sb.Append(arg.name).Append(" : ").Append(arg.type.ToString());
            }
            sb.Append(")");
            if (rettype != null)
                sb.Append(" : ").Append(rettype.ToString());

            return sb.ToString();
        }

        public IEnumerable<Statement> GetStatements()
        {
            foreach (Statement st in body.GetSubStatements())
                yield return st;
        }
    }

    /// <summary>
    /// </summary>
    public class FunctionTable : List<FunctionDefinition>
    {
    }
}
