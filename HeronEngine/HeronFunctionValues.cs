﻿/// Heron language interpreter for Windows in C#
/// http://www.heron-language.com
/// Copyright (c) 2009 Christopher Diggins
/// Licenced under the MIT License 1.0 
/// http://www.opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace HeronEngine
{
    /// <summary>
    /// Represents an object that can be applied to arguments (i.e. called)
    /// You can think of this as a member function bound to the this argument.
    /// In C# this would be called a delegate.
    /// </summary>
    public class FunctionValue : HeronValue
    {
        HeronValue self;
        Scope freeVars;
        FunctionDefn fun;

        public FunctionValue(HeronValue self, FunctionDefn f)
        {
            this.self = self;
            fun = f;
        }

        /// <summary>
        /// This is a second stage of construction needed for closures. 
        /// It computes any free variables in the function, and binds them 
        /// to values
        /// </summary>
        /// <param name="vm"></param>
        public void ComputeFreeVars(VM vm)
        {
            freeVars = new Scope();
            var boundVars = new Stack<string>();
            boundVars.Push(fun.name);
            foreach (FormalArg arg in fun.formals)
                boundVars.Push(arg.name);
            GetFreeVars(vm, fun.body, boundVars, freeVars);
        }

        private void GetFreeVars(VM vm, Statement st, Stack<string> boundVars, Scope result)
        {
            // InternalCount and store the names defined by this statement 
            int nNewVars = 0;
            foreach (string name in st.GetDefinedNames())
            {
                ++nNewVars;
                boundVars.Push(name);
            }

            // Iterate over all names used by expressions in the statement
            foreach (string name in st.GetUsedNames())
            {
                // Is this not a boundVar, and not already marked as a free var
                if (!boundVars.Contains(name) && !result.HasName(name))
                {
                    // Throws an exception if the name is not found
                    HeronValue v = vm.LookupName(name);
                    result.Add(new VarDesc(name), v);
                }
            }

            // Recurse over all sub statements, getting the free vars
            foreach (Statement child in st.GetSubStatements())
                GetFreeVars(vm, child, boundVars, result);

            // Pop all variables added by this variable
            for (int i = 0; i < nNewVars; ++i)
                boundVars.Pop();
        }

        private void PushArgs(VM vm, HeronValue[] args)
        {
            int n = fun.formals.Count;
            Debug.Assert(n == args.Length);
            for (int i = 0; i < n; ++i)
            {
                vm.AddVar(fun.formals[i], args[i]);
            }
        }

        public ClassInstance GetClassInstance()
        {
            return self as ClassInstance;
        }

        public ModuleInstance GetModuleInstance()
        {
            ModuleInstance mi = self as ModuleInstance;
            if (mi != null)
                return mi;

            ClassInstance ci = GetClassInstance();
            if (ci != null)
                return ci.GetModuleInstance();

            return null;
        }

        public void PerformCoercions(HeronValue[] xs)
        {
            for (int i = 0; i < xs.Length; ++i)
                xs[i] = fun.formals[i].Coerce(xs[i]);
        }

        public HeronType GetFormalType(int n)
        {
            return fun.formals[n].type.type;
        }

        public override HeronValue Apply(VM vm, HeronValue[] args)
        {
            // Create a stack frame 
            using (vm.CreateFrame(fun, GetClassInstance(), GetModuleInstance()))
            {
                try
                {
                    // Convert the arguments into appropriate types
                    PerformCoercions(args);

                    // Push the arguments into the current scope
                    PushArgs(vm, args);

                    // Copy free vars
                    if (freeVars != null)
                        vm.AddVars(freeVars);

                    // Eval the function body
                    vm.Eval(fun.body);
                }
                catch (Exception e)
                {
                    throw new VMException(fun, fun.FileName, vm.CurrentStatement, e);
                }
            }

            // Gets last result and resets it
            return vm.GetAndResetResult();
        }

        public override HeronType Type
        {
            get { return PrimitiveTypes.FunctionType; }
        }

        public FunctionDefn GetDefn()
        {
            return fun;
        }
    }

    /// <summary>
    /// Represents a group of similiar functions. 
    /// In most cases these would all have the same name. 
    /// This is class is used for dynamic resolution of overloaded function
    /// names.
    /// </summary>
    public class FunDefnListValue : HeronValue
    {
        HeronValue self;
        string name;
        List<FunctionDefn> functions = new List<FunctionDefn>();

        public int Count
        {
            get
            {
                return functions.Count;
            }
        }

        public FunDefnListValue(HeronValue self, string name, IEnumerable<FunctionDefn> funcs)
        {
            this.self = self;
            foreach (FunctionDefn f in funcs)
                functions.Add(f);
            this.name = name;
            foreach (FunctionDefn f in functions)
                if (f.name != name)
                    throw new Exception("All functions in function list object must share the same name");
        }

        /// <summary>
        /// This is a very primitive resolution function that only looks at the number of arguments 
        /// provided. A more sophisticated function would look at the types.
        /// </summary>
        /// <param name="funcs"></param>
        /// <returns></returns>
        public FunctionValue Resolve(VM vm, HeronValue[] args)
        {
            List<FunctionValue> r = new List<FunctionValue>();
            foreach (FunctionDefn f in functions)
            {
                if (f.formals.Count == args.Length)
                {
                    r.Add(new FunctionValue(self, f));
                }
            }
            if (r.Count == 0)
                return null;
            else if (r.Count == 1)
                return r[0];
            else
                return FindBestMatch(r, args);
        }

        /// <summary>
        /// Given a list of arguments, it looks at the types and tries to find out which function 
        /// in this list is the best match.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="funcs"></param>
        /// <returns></returns>
        public FunctionValue FindBestMatch(List<FunctionValue> list, HeronValue[] args)
        {
            // Each iteration removes candidates. This list holds all of the matches
            // Necessary, because removing items from a list while we iterate it is hard.
            List<FunctionValue> tmp = new List<FunctionValue>(list);
            for (int pos = 0; pos < args.Length; ++pos)
            {
                // On each iteration, update the main list, to only contain the remaining items
                list = new List<FunctionValue>(tmp);
                HeronValue arg = args[pos];
                HeronType argType;
                if (arg is AnyValue)
                    argType = (arg as AnyValue).GetHeldType();
                else
                    argType = arg.Type;
                for (int i = 0; i < list.Count; ++i)
                {
                    FunctionValue fo = list[i];
                    HeronType formalType = fo.GetFormalType(pos);
                    if (!formalType.Equals(argType))
                        tmp.Remove(fo);
                }
                if (tmp.Count == 0)
                    throw new Exception("Could not resolve function " + name + " no function matches perfectly");

                // We found a single best match
                if (tmp.Count == 1)
                    return tmp[0];
            }

            Debug.Assert(tmp.Count > 1);
            
            // There are multiple functions
            // Choose the function which is the most derived type.
            // This is a pretty awful hack to make up for a bad design. 
            int n = 0;
            FunctionValue best = null;
            foreach (FunctionValue fv in list)
            {
                int depth = fv.GetDefn().parent.GetHierarchyDepth();
                if (depth > n)
                {
                    n = depth;
                    best = fv;
                }
                else if (depth == n)
                {
                    // Ambiguous match at this depth.
                    best = null;
                }
            }

            if (best == null)
                throw new Exception("Ambiguous function match");

            return best;
        }

        public override HeronValue Apply(VM vm, HeronValue[] args)
        {
            Debug.Assert(functions.Count > 0);
            FunctionValue o = Resolve(vm, args);
            if (o == null)
                throw new Exception("Could not resolve function '" + name + "' with arguments " + ArgsToString(args));
            return o.Apply(vm, args);
        }

        public static string ArgsToString(HeronValue[] args)
        {
            string r = "(";
            for (int i = 0; i < args.Length; ++i)
            {
                if (i > 0) r += ", ";
                r += args[i].ToString();
            }
            r += ")";
            return r;
        }

        public override HeronType Type
        {
            get { return PrimitiveTypes.FunctionListType; }
        }

        public List<FunctionDefn> GetDefns()
        {
            return functions;
        }
    }

    /// <summary>
    /// Represents a member function that is bound to the "this" value.
    /// Just like a C# delegate. Note that when you get a method from a PrimitiveValue
    /// you have to convert it to a BoundMethodValue.
    /// </summary>
    public class BoundMethodValue : HeronValue
    {
        HeronValue self;
        ExposedMethodValue method;

        public BoundMethodValue(HeronValue self, ExposedMethodValue method)
        {
            this.self = self;
            this.method = method;
        }

        public override HeronValue Apply(VM vm, HeronValue[] args)
        {
            return method.Invoke(vm, self, args);
        }

        public override HeronType Type
        {
            get { return PrimitiveTypes.BoundMethodType; }
        }
    }
}