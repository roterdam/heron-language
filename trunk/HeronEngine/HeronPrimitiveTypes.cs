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
    static class PrimitiveTypes
    {
        public static HeronType TypeType = new HeronType(null, typeof(HeronType), "Type");
        public static HeronType AnyType = new HeronType(null, typeof(Any), "Any");
        public static HeronType VoidType = new HeronType(null, typeof(VoidValue), "Void");
        public static HeronType UndefinedType = new HeronType(null, typeof(UndefinedValue), "Undefined");
        public static HeronType NullType = new HeronType(null, typeof(NullValue), "Null");
        public static HeronType BoolType = new HeronType(null, typeof(BoolValue), "Bool");
        public static HeronType IntType = new HeronType(null, typeof(IntValue), "Int");
        public static HeronType FloatType = new HeronType(null, typeof(FloatValue), "Float");
        public static HeronType CharType = new HeronType(null, typeof(CharValue), "Char");
        public static HeronType StringType = new HeronType(null, typeof(StringValue), "String");
        public static HeronType IteratorType = new HeronType(null, typeof(IteratorValue), "Iterator");
        public static HeronType SeqType = new HeronType(null, typeof(SeqValue), "Seq");
        public static HeronType ListType = new HeronType(null, typeof(ListValue), "List");
        public static HeronType TreeType = new HeronType(null, typeof(ListValue), "Tree");

        public static HeronType FunctionType = new HeronType(null, typeof(FunctionValue), "Function");
        public static HeronType ExposedMethodType = new HeronType(null, typeof(ExposedMethod), "PrimitiveMethod");
        public static HeronType FunctionListType = new HeronType(null, typeof(FunDefnListValue), "FunctionList");
        public static HeronType BoundMethodType = new HeronType(null, typeof(BoundMethod), "BoundMethod");
        public static HeronType ExternalMethodType = new HeronType(null, typeof(DotNetMethod), "ExternalMethod");
        public static HeronType ExternalStaticMethodListType = new HeronType(null, typeof(DotNetStaticMethodGroup), "ExternalStaticMethodList");
        public static HeronType ExternalMethodListType = new HeronType(null, typeof(DotNetMethodGroup), "ExternalMethodList");

        public static HeronType ProgramType = new HeronType(null, typeof(HeronProgram), "HeronProgram");
        public static HeronType ModuleType = new HeronType(null, typeof(HeronModule), "HeronModule");
        public static HeronType ClassType = new HeronType(null, typeof(HeronClass), "HeronClass");
        public static HeronType InterfaceType = new HeronType(null, typeof(HeronInterface), "HeronInterface");
        public static HeronType EnumType = new HeronType(null, typeof(HeronEnum), "HeronEnum");
        public static HeronType FieldDefnType = new HeronType(null, typeof(FieldDefn), "FieldDefn");
        public static HeronType FunctionDefnType = new HeronType(null, typeof(FunctionDefn), "FunctionDefn");
        public static HeronType FormalArg = new HeronType(null, typeof(FormalArg), "FormalArg");

        static Dictionary<string, HeronType> types = null;

        static void AddType(HeronType prim)
        {
            Trace.Assert(prim != null);
            types.Add(prim.name, prim);
        }

        static public Dictionary<string, HeronType> GetTypes()
        {
            if (types == null)
            {
                types = new Dictionary<string, HeronType>();
                foreach (FieldInfo fi in typeof(PrimitiveTypes).GetFields())
                    if (fi.FieldType.Equals(typeof(HeronType)) && fi.IsStatic)
                        AddType(fi.GetValue(null) as HeronType);
            }

            return types;
        }
    }
}
