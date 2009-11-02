/// Heron language interpreter for Windows in C#
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
    public class PrimitiveType : HeronType
    {
        /// <summary>
        /// Can be null.
        /// </summary>
        Type type;

        /// <summary>
        /// Maps names to functions
        /// </summary>
        Dictionary<string, Method> functions = new Dictionary<string, Method>();

        public PrimitiveType(HeronModule m, Type t, string name)
            : base(m, name)
        {
            if (t != null)
            {
                if (!typeof(HeronValue).IsAssignableFrom(t))
                    throw new Exception("Only types derived from HeronValue can be primitives");
            }
            this.type = t;
            StoreExposedFunctions();
        }

        private void StoreExposedFunctions()
        {
            if (type == null)
                return;

            foreach (MethodInfo mi in type.GetMethods()) 
            {
                object[] attrs = mi.GetCustomAttributes(typeof(HeronVisible), true);
                if (attrs.Length > 0)
                    StoreFunction(mi);
            }
        }

        private void StoreFunction(MethodInfo mi)
        {
            PrimitiveMethod m = new PrimitiveMethod(mi);
            functions.Add(m.Name, m);
        }

        public override HeronValue Instantiate(VM vm, HeronValue[] args)
        {
            if (args.Length != 0)
                throw new Exception("arguments not supported when instantiating primitive type " + name);

            if (type == null)
                throw new Exception("type " + name + " can't be instantiated");
 
            Object r = type.InvokeMember(null, System.Reflection.BindingFlags.CreateInstance, null, null, new Object[] { });
            return r as HeronValue;
        }

        public Method GetMethod(string name)
        {
            return functions[name];
        }
    }

    static class PrimitiveTypes
    {
        public static PrimitiveType TypeType = new PrimitiveType(null, typeof(HeronType), "Type");
        public static PrimitiveType AnyType = new PrimitiveType(null, null, "Any");
        public static PrimitiveType VoidType = new PrimitiveType(null, null, "Void");
        public static PrimitiveType UndefinedType = new PrimitiveType(null, null, "Undefined");
        public static PrimitiveType NullType = new PrimitiveType(null, null, "Null");
        public static PrimitiveType BoolType = new PrimitiveType(null, typeof(BoolValue), "Bool");
        public static PrimitiveType IntType = new PrimitiveType(null, typeof(IntValue), "Int");
        public static PrimitiveType FloatType = new PrimitiveType(null, typeof(FloatValue), "Float");
        public static PrimitiveType CharType = new PrimitiveType(null, typeof(CharValue), "Char");
        public static PrimitiveType StringType = new PrimitiveType(null, typeof(StringValue), "String");
        public static PrimitiveType IteratorType = new PrimitiveType(null, null, "Iterator");
        public static PrimitiveType SeqType = new PrimitiveType(null, typeof(SeqValue), "Seq");
        public static PrimitiveType ListType = new PrimitiveType(null, typeof(ListValue), "List");

        public static PrimitiveType PrimitiveMethodType = new PrimitiveType(null, typeof(PrimitiveMethod), "PrimitiveMethod");
        public static PrimitiveType FunctionType = new PrimitiveType(null, null, "Function");
        public static PrimitiveType FunctionListType = new PrimitiveType(null, null, "FunctionList");
        public static PrimitiveType BoundMethodType = new PrimitiveType(null, typeof(BoundMethod), "BoundMethod");
        public static PrimitiveType MethodType = new PrimitiveType(null, typeof(Method), "Method");
        public static PrimitiveType ExternalMethodType = new PrimitiveType(null, null, "ExternalMethod");
        public static PrimitiveType ExternalStaticMethodListType = new PrimitiveType(null, null, "ExternalStaticMethodList");
        public static PrimitiveType ExternalMethodListType = new PrimitiveType(null, null, "ExternalMethodList");

        public static PrimitiveType ProgramType = new PrimitiveType(null, null, "HeronProgram");
        public static PrimitiveType ModuleType = new PrimitiveType(null, null, "HeronModule");
        public static PrimitiveType ClassType = new PrimitiveType(null, null, "HeronClass");
        public static PrimitiveType EnumType = new PrimitiveType(null, null, "HeronEnum");
        public static PrimitiveType InterfaceType = new PrimitiveType(null, null, "HeronInterface");

        static Dictionary<string, PrimitiveType> types = null;

        static void AddType(PrimitiveType prim)
        {
            types.Add(prim.name, prim);
        }

        static public Dictionary<string, PrimitiveType> GetTypes()
        {
            if (types == null)
            {
                types = new Dictionary<string, PrimitiveType>();
                AddType(AnyType);
                AddType(VoidType);
                AddType(UndefinedType);
                AddType(NullType);
                AddType(BoolType);
                AddType(IntType);
                AddType(FloatType);
                AddType(CharType);
                AddType(StringType);
                //AddType(CollectionType);
                AddType(FunctionType);
                AddType(FunctionListType);
                AddType(ExternalMethodType);
                AddType(ExternalStaticMethodListType);
                AddType(ExternalMethodListType);

                AddType(ProgramType);
                AddType(ModuleType);
                AddType(ClassType);
                AddType(EnumType);
                AddType(InterfaceType);
            }

            return types;
        }
    }
}
