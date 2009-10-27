/// Heron language interpreter for Windows in C#
/// http://www.heron-language.com
/// Copyright (c) 2009 Christopher Diggins
/// Licenced under the MIT License 1.0 
/// http://www.opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    public class HeronPrimitiveType : HeronType
    {
        public HeronPrimitiveType(HeronModule m, string name)
            : base(m, name)
        {
        }

        public override HeronValue Instantiate(HeronVM vm, HeronValue[] args)
        {
            if (args.Length != 0)
                throw new Exception("arguments not supported when instantiating primitives");

            switch (name)
            {
                case "Int":
                    return new IntValue();
                case "Float":
                    return new FloatValue();
                case "Char":
                    return new CharValue();
                case "String":
                    return new StringValue();
                case "Collection":
                    return DotNetObject.Marshal(new HeronCollection());
                default:
                    throw new Exception("Unhandled primitive type " + name);
            }
        }
    }

    static class HeronPrimitiveTypes
    {
        public static HeronPrimitiveType TypeType = new HeronPrimitiveType(null, "Type");
        public static HeronPrimitiveType AnyType = new HeronPrimitiveType(null, "Any");
        public static HeronPrimitiveType VoidType = new HeronPrimitiveType(null, "Void");
        public static HeronPrimitiveType UndefinedType = new HeronPrimitiveType(null, "Undefined");
        public static HeronPrimitiveType NullType = new HeronPrimitiveType(null, "Null");
        public static HeronPrimitiveType BoolType = new HeronPrimitiveType(null, "Bool");
        public static HeronPrimitiveType IntType = new HeronPrimitiveType(null, "Int");
        public static HeronPrimitiveType FloatType = new HeronPrimitiveType(null, "Float");
        public static HeronPrimitiveType CharType = new HeronPrimitiveType(null, "Char");
        public static HeronPrimitiveType StringType = new HeronPrimitiveType(null, "String");
        //public static Heron`PrimitiveType CollectionType = new HeronPrimitiveType(null, "Collection");
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
