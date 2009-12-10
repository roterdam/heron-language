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

        public static HeronType ProgramType = new HeronType(typeof(HeronProgram));
        public static HeronType ModuleType = new HeronType(typeof(HeronModule));
        public static HeronType ClassType = new HeronType(typeof(HeronClass));
        public static HeronType InterfaceType = new HeronType(typeof(HeronInterface));
        public static HeronType EnumType = new HeronType(typeof(HeronEnum));
        public static HeronType FieldDefnType = new HeronType(typeof(FieldDefn));
        public static HeronType FunctionDefnType = new HeronType(typeof(FunctionDefn));
        public static HeronType FormalArg = new HeronType(typeof(FormalArg));

        public static HeronType VariableDeclaration = new HeronType(typeof(VariableDeclaration));
        public static HeronType DeleteStatement = new HeronType(typeof(DeleteStatement));
        public static HeronType ExpressionStatement = new HeronType(typeof(ExpressionStatement));
        public static HeronType ForEachStatement = new HeronType(typeof(ForEachStatement));
        public static HeronType ForStatement = new HeronType(typeof(ForStatement));
        public static HeronType CodeBlock = new HeronType(typeof(CodeBlock));
        public static HeronType IfStatement = new HeronType(typeof(IfStatement));
        public static HeronType WhileStatement = new HeronType(typeof(WhileStatement));
        public static HeronType ReturnStatement = new HeronType(typeof(ReturnStatement));
        public static HeronType SwitchStatement = new HeronType(typeof(SwitchStatement));
        public static HeronType CaseStatement = new HeronType(typeof(CaseStatement));

        public static HeronType Assignment = new HeronType(typeof(Assignment));
        public static HeronType ChooseField = new HeronType(typeof(ChooseField));
        public static HeronType ReadAt = new HeronType(typeof(ReadAt));
        public static HeronType NewExpr = new HeronType(typeof(NewExpr));
        public static HeronType NullExpr = new HeronType(typeof(NullExpr));
        public static HeronType IntLiteral = new HeronType(typeof(IntLiteral));
        public static HeronType BoolLiteral = new HeronType(typeof(BoolLiteral));
        public static HeronType FloatLiteral = new HeronType(typeof(FloatLiteral));
        public static HeronType CharLiteral = new HeronType(typeof(CharLiteral));
        public static HeronType StringLiteral = new HeronType(typeof(StringLiteral));
        public static HeronType Name = new HeronType(typeof(Name));
        public static HeronType FunCall = new HeronType(typeof(FunCall));
        public static HeronType UnaryOperation = new HeronType(typeof(UnaryOperation));
        public static HeronType BinaryOperation = new HeronType(typeof(BinaryOperation));
        public static HeronType AnonFunExpr = new HeronType(typeof(AnonFunExpr));
        public static HeronType PostIncExpr = new HeronType(typeof(PostIncExpr));
        public static HeronType SelectExpr = new HeronType(typeof(SelectExpr));
        public static HeronType MapEachExpr = new HeronType(typeof(MapEachExpr));
        public static HeronType AccumulateExpr = new HeronType(typeof(AccumulateExpr));
        public static HeronType TupleExpr = new HeronType(typeof(TupleExpr));

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

        static void AppendFields(StringBuilder sb, HeronType t)
        {
            sb.AppendLine("  fields");
            sb.AppendLine("  {");
            foreach (ExposedField f in t.GetExposedFields())
                sb.AppendLine("    " + f.ToString() + ";");             
            sb.AppendLine("  }");
        }

        static void AppendMethods(StringBuilder sb, HeronType t)
        {
            sb.AppendLine("  methods");
            sb.AppendLine("  {");
            foreach (ExposedMethod m in t.GetExposedMethods())
                sb.AppendLine("    " + m.ToString() + ";");
            sb.AppendLine("  }");
        }

        static public string AsString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (HeronType t in GetTypes().Values)
            {
                sb.AppendLine("primitive " + t.name);
                sb.AppendLine("{");
                AppendFields(sb, t);
                AppendMethods(sb, t);
                sb.AppendLine("}");
            }
            return sb.ToString();
        }
    }
}
