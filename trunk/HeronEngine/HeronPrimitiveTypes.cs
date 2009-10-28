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
    public class HeronPrimitiveType : HeronType
    {
        Type type;

        public HeronPrimitiveType(HeronModule m, Type t, string name)
            : base(m, name)
        {
            Trace.Assert(t.Equals(typeof(HeronValue)));
            this.type = t;
        }

        public override HeronValue Instantiate(HeronVM vm, HeronValue[] args)
        {
            if (args.Length != 0)
                throw new Exception("arguments not supported when instantiating primitive type " + name);

            if (type == null)
                throw new Exception("type " + name + " can't be instantiated");
 
            Object r = type.InvokeMember(null, System.Reflection.BindingFlags.CreateInstance, null, null, new Object[] { });
            return r as HeronValue;
        }
    }

    static class HeronPrimitiveTypes
    {
        public static HeronPrimitiveType TypeType = new HeronPrimitiveType(null, typeof(HeronType), "Type");
        public static HeronPrimitiveType AnyType = new HeronPrimitiveType(null, null, "Any");
        public static HeronPrimitiveType VoidType = new HeronPrimitiveType(null, null, "Void");
        public static HeronPrimitiveType UndefinedType = new HeronPrimitiveType(null, null, "Undefined");
        public static HeronPrimitiveType NullType = new HeronPrimitiveType(null, null, "Null");
        public static HeronPrimitiveType BoolType = new HeronPrimitiveType(null, typeof(BoolValue), "Bool");
        public static HeronPrimitiveType IntType = new HeronPrimitiveType(null, typeof(IntValue), "Int");
        public static HeronPrimitiveType FloatType = new HeronPrimitiveType(null, typeof(FloatValue), "Float");
        public static HeronPrimitiveType CharType = new HeronPrimitiveType(null, typeof(CharValue), "Char");
        public static HeronPrimitiveType StringType = new HeronPrimitiveType(null, typeof(StringValue), "String");
        public static HeronPrimitiveType SeqType = new HeronPrimitiveType(null, typeof(SeqValue), "Seq");
        public static HeronPrimitiveType ListType = new HeronPrimitiveType(null, typeof(ListValue), "List");

        /*
        public static HeronPrimitiveType FunctionType = new HeronPrimitiveType(null, "Function");
        public static HeronPrimitiveType FunctionListType = new HeronPrimitiveType(null, "FunctionList");
        public static HeronPrimitiveType ExternalMethodType = new HeronPrimitiveType(null, "ExternalMethod");
        public static HeronPrimitiveType ExternalStaticMethodListType = new HeronPrimitiveType(null, "ExternalStaticMethodList");
        public static HeronPrimitiveType ExternalMethodListType = new HeronPrimitiveType(null, "ExternalMethodList");
        
        public static HeronPrimitiveType ProgramType = new HeronPrimitiveType(null, "HeronProgram");
        public static HeronPrimitiveType ModuleType = new HeronPrimitiveType(null, "HeronModule");
        public static HeronPrimitiveType ClassType = new HeronPrimitiveType(null, "HeronClass");
        public static HeronPrimitiveType EnumType = new HeronPrimitiveType(null, "HeronEnum");
        public static HeronPrimitiveType InterfaceType = new HeronPrimitiveType(null, "HeronInterface");
        */

        static Dictionary<string, HeronPrimitiveType> types = null;

        static void AddType(HeronPrimitiveType prim)
        {
            types.Add(prim.name, prim);
        }

        static public Dictionary<string, HeronPrimitiveType> GetTypes()
        {
            if (types == null)
            {
                types = new Dictionary<string, HeronPrimitiveType>();
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
