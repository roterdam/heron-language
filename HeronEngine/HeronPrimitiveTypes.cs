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
        // The specials
        public static HeronType VoidType = new HeronType(null, typeof(VoidValue), "Void");
        public static HeronType NullType = new HeronType(null, typeof(NullValue), "Null");

        // Usual suspects 
        public static HeronType BoolType = new HeronType(null, typeof(BoolValue), "Bool");
        public static HeronType IntType = new HeronType(null, typeof(IntValue), "Int");
        public static HeronType FloatType = new HeronType(null, typeof(FloatValue), "Float");
        public static HeronType CharType = new HeronType(null, typeof(CharValue), "Char");
        public static HeronType StringType = new HeronType(null, typeof(StringValue), "String");
        
        // Heron collection types
        public static HeronType IteratorType = new HeronType(null, typeof(IteratorValue), "Iterator");
        public static HeronType SeqType = new HeronType(null, typeof(SeqValue), "Seq");
        public static HeronType ListType = new HeronType(null, typeof(ListValue), "List");

        // Not currently supported
        public static HeronType TreeType = new HeronType(null, typeof(ListValue), "Tree");

        // Misc types
        public static HeronType TypeType = new HeronType(null, typeof(HeronType), "Type");
        public static HeronType AnyType = new HeronType(null, typeof(AnyValue), "Any");

        // Function types
        public static HeronType FunctionType = new HeronType(null, typeof(FunctionValue), "Function");
        public static HeronType FunctionListType = new HeronType(null, typeof(FunDefnListValue), "FunctionList");
        public static HeronType BoundMethodType = new HeronType(null, typeof(BoundMethodValue), "BoundMethod");
        public static HeronType ExposedMethodType = new HeronType(null, typeof(ExposedMethodValue), "PrimitiveMethod");

        // FFI types
        public static HeronType ExternalMethodType = new HeronType(null, typeof(DotNetMethod), "ExternalMethod");
        public static HeronType ExternalStaticMethodListType = new HeronType(null, typeof(DotNetStaticMethodGroup), "ExternalStaticMethodList");
        public static HeronType ExternalMethodListType = new HeronType(null, typeof(DotNetMethodGroup), "ExternalMethodList");

        // Code model types
        public static HeronCodeModelType ProgramType = new HeronCodeModelType(typeof(ProgramDefn));
        public static HeronCodeModelType ModuleType = new HeronCodeModelType(typeof(ModuleDefn));
        public static HeronCodeModelType ClassType = new HeronCodeModelType(typeof(ClassDefn));
        public static HeronCodeModelType InterfaceType = new HeronCodeModelType(typeof(InterfaceDefn));
        public static HeronCodeModelType EnumType = new HeronCodeModelType(typeof(EnumDefn));
        public static HeronCodeModelType FieldDefnType = new HeronCodeModelType(typeof(FieldDefn));
        public static HeronCodeModelType FunctionDefnType = new HeronCodeModelType(typeof(FunctionDefn));
        public static HeronCodeModelType FormalArg = new HeronCodeModelType(typeof(FormalArg));

        // Code model statement types
        public static HeronCodeModelType VariableDeclaration = new HeronCodeModelType(typeof(VariableDeclaration));
        public static HeronCodeModelType DeleteStatement = new HeronCodeModelType(typeof(DeleteStatement));
        public static HeronCodeModelType ExpressionStatement = new HeronCodeModelType(typeof(ExpressionStatement));
        public static HeronCodeModelType ForEachStatement = new HeronCodeModelType(typeof(ForEachStatement));
        public static HeronCodeModelType ForStatement = new HeronCodeModelType(typeof(ForStatement));
        public static HeronCodeModelType CodeBlock = new HeronCodeModelType(typeof(CodeBlock));
        public static HeronCodeModelType IfStatement = new HeronCodeModelType(typeof(IfStatement));
        public static HeronCodeModelType WhileStatement = new HeronCodeModelType(typeof(WhileStatement));
        public static HeronCodeModelType ReturnStatement = new HeronCodeModelType(typeof(ReturnStatement));
        public static HeronCodeModelType SwitchStatement = new HeronCodeModelType(typeof(SwitchStatement));
        public static HeronCodeModelType CaseStatement = new HeronCodeModelType(typeof(CaseStatement));

        // Code model expresssion types
        public static HeronCodeModelType Assignment = new HeronCodeModelType(typeof(Assignment));
        public static HeronCodeModelType ChooseField = new HeronCodeModelType(typeof(ChooseField));
        public static HeronCodeModelType ReadAt = new HeronCodeModelType(typeof(ReadAt));
        public static HeronCodeModelType NewExpr = new HeronCodeModelType(typeof(NewExpr));
        public static HeronCodeModelType NullExpr = new HeronCodeModelType(typeof(NullExpr));
        public static HeronCodeModelType IntLiteral = new HeronCodeModelType(typeof(IntLiteral));
        public static HeronCodeModelType BoolLiteral = new HeronCodeModelType(typeof(BoolLiteral));
        public static HeronCodeModelType FloatLiteral = new HeronCodeModelType(typeof(FloatLiteral));
        public static HeronCodeModelType CharLiteral = new HeronCodeModelType(typeof(CharLiteral));
        public static HeronCodeModelType StringLiteral = new HeronCodeModelType(typeof(StringLiteral));
        public static HeronCodeModelType Name = new HeronCodeModelType(typeof(Name));
        public static HeronCodeModelType FunCall = new HeronCodeModelType(typeof(FunCall));
        public static HeronCodeModelType UnaryOperation = new HeronCodeModelType(typeof(UnaryOperation));
        public static HeronCodeModelType BinaryOperation = new HeronCodeModelType(typeof(BinaryOperation));
        public static HeronCodeModelType AnonFunExpr = new HeronCodeModelType(typeof(AnonFunExpr));
        public static HeronCodeModelType PostIncExpr = new HeronCodeModelType(typeof(PostIncExpr));
        public static HeronCodeModelType SelectExpr = new HeronCodeModelType(typeof(SelectExpr));
        public static HeronCodeModelType MapEachExpr = new HeronCodeModelType(typeof(MapEachExpr));
        public static HeronCodeModelType AccumulateExpr = new HeronCodeModelType(typeof(AccumulateExpr));
        public static HeronCodeModelType TupleExpr = new HeronCodeModelType(typeof(TupleExpr));

        static SortedDictionary<string, HeronType> types = null;

        static void AddType(HeronType prim)
        {
            Trace.Assert(prim != null);
            types.Add(prim.name, prim);
        }

        static public SortedDictionary<string, HeronType> GetTypes()
        {
            if (types == null)
            {
                types = new SortedDictionary<string, HeronType>();
                foreach (FieldInfo fi in typeof(PrimitiveTypes).GetFields())
                    if (typeof(HeronType).IsAssignableFrom(fi.FieldType) && fi.IsStatic)
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
            foreach (ExposedMethodValue m in t.GetExposedMethods())
                sb.AppendLine("    " + m.ToString() + ";");
            sb.AppendLine("  }");
        }

        static public string NonCodeModelTypesAsString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (HeronType t in GetTypes().Values)
            {
                if (!(t is HeronCodeModelType))
                {
                    sb.AppendLine("primitive " + t.name);
                    sb.AppendLine("{");
                    AppendFields(sb, t);
                    AppendMethods(sb, t);
                    sb.AppendLine("}");
                }
            } 
            return sb.ToString();
        }

        static public string CodeModelTypesAsString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (HeronType t in GetTypes().Values)
            {
                if (t is HeronCodeModelType)
                {
                    sb.AppendLine("primitive " + t.name);
                    sb.AppendLine("{");
                    AppendFields(sb, t);
                    AppendMethods(sb, t);
                    sb.AppendLine("}");
                }
            }
            return sb.ToString();
        }
    }
}
