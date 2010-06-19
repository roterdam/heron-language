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
        // This is not an official feature of Heron.
        public static HeronType VMType = new HeronType(null, typeof(VM), "VM");

        // Compiler services
        public static HeronType CodeModelBuilderType = new HeronType(null, typeof(CodeModelBuilder), "CodeModelBuilder");

        // Special types
        public static HeronType VoidType = new HeronType(null, typeof(VoidValue), "Void");
        public static HeronType NullType = new HeronType(null, typeof(NullValue), "Null");
        public static HeronType TypeValueType = new HeronType(null, typeof(TypeValue), "TypeValue");
        public static HeronType AnyType = new HeronType(null, typeof(AnyValue), "Any");
        public static HeronType UnknownType = new HeronType(null, typeof(VoidValue), "Unknown");
        public static HeronType OptimizedExpressionType = new HeronType(null, typeof(OptimizedExpression), "Optimized");

        // Usual suspects 
        public static HeronType BoolType = new HeronType(null, typeof(BoolValue), "Bool");
        public static HeronType IntType = new HeronType(null, typeof(IntValue), "Int");
        public static HeronType FloatType = new HeronType(null, typeof(FloatValue), "Float");
        public static HeronType CharType = new HeronType(null, typeof(CharValue), "Char");
        public static HeronType StringType = new HeronType(null, typeof(StringValue), "String");
        
        // Heron collection types
        public static HeronType SeqType = new HeronType(null, typeof(SeqValue), "Seq");
        public static HeronType IteratorType = new HeronType(SeqType, null, typeof(IteratorValue), "Iterator");
        public static HeronType ListType = new HeronType(SeqType, null, typeof(ListValue), "List");
        public static HeronType ArrayType = new HeronType(SeqType, null, typeof(ArrayValue), "Array");
        public static HeronType RecordType = new HeronType(SeqType, null, typeof(RecordValue), "Record");
        public static HeronType TableType = new HeronType(SeqType, null, typeof(TableValue), "Table");
        public static HeronType SliceType = new HeronType(SeqType, null, typeof(SliceValue), "Slice");

        // Function types
        public static HeronType FunctionType = new HeronType(null, typeof(FunctionValue), "Function");
        public static HeronType FunctionListType = new HeronType(null, typeof(FunDefnListValue), "FunctionList");
        public static HeronType BoundMethodType = new HeronType(null, typeof(BoundMethodValue), "BoundMethod");
        public static HeronType ExposedMethodType = new HeronType(null, typeof(ExposedMethodValue), "PrimitiveMethod");

        // FFI types
        public static HeronType ExternalListType = new HeronType(SeqType, null, typeof(DotNetList), "ExternalList");
        public static HeronType ExternalStaticMethodListType = new HeronType(null, typeof(DotNetStaticMethodGroup), "ExternalStaticMethodList");
        public static HeronType ExternalMethodListType = new HeronType(null, typeof(DotNetMethodGroup), "ExternalMethodList");
        public static HeronType ExternalClass = new HeronType(null, typeof(DotNetClass), "ExternalClass");

        // Code model types
        public static CodeModelType VarDescType = new CodeModelType(typeof(VarDesc));
        public static CodeModelType ProgramType = new CodeModelType(typeof(ProgramDefn));
        public static CodeModelType TypeType = new CodeModelType(typeof(HeronType), "TypeType");
        public static CodeModelType TypeRefType = new CodeModelType(typeof(TypeRef));
        public static CodeModelType ClassType = new CodeModelType(TypeType, typeof(ClassDefn));
        public static CodeModelType ModuleType = new CodeModelType(ClassType, typeof(ModuleDefn));
        public static CodeModelType InterfaceType = new CodeModelType(TypeType, typeof(InterfaceDefn));
        public static CodeModelType EnumType = new CodeModelType(TypeType, typeof(EnumDefn));
        public static CodeModelType FieldDefnType = new CodeModelType(VarDescType, typeof(FieldDefn));
        public static CodeModelType ImportType = new CodeModelType(typeof(ModuleDefn.Import));
        public static CodeModelType FunctionDefnType = new CodeModelType(typeof(FunctionDefn));
        public static CodeModelType FormalArgType = new CodeModelType(VarDescType, typeof(FormalArg));

        // Code model statement types
        public static CodeModelType Statement = new CodeModelType(typeof(Statement));
        public static CodeModelType VariableDeclaration = new CodeModelType(Statement, typeof(VariableDeclaration));
        public static CodeModelType DeleteStatement = new CodeModelType(Statement, typeof(DeleteStatement));
        public static CodeModelType ExpressionStatement = new CodeModelType(Statement, typeof(ExpressionStatement));
        public static CodeModelType ForEachStatement = new CodeModelType(Statement, typeof(ForEachStatement));
        public static CodeModelType ForStatement = new CodeModelType(Statement, typeof(ForStatement));
        public static CodeModelType CodeBlock = new CodeModelType(Statement, typeof(CodeBlock));
        public static CodeModelType IfStatement = new CodeModelType(Statement, typeof(IfStatement));
        public static CodeModelType WhileStatement = new CodeModelType(Statement, typeof(WhileStatement));
        public static CodeModelType ReturnStatement = new CodeModelType(Statement, typeof(ReturnStatement));
        public static CodeModelType SwitchStatement = new CodeModelType(Statement, typeof(SwitchStatement));
        public static CodeModelType CaseStatement = new CodeModelType(Statement, typeof(CaseStatement));

        // Code model expresssion types
        public static CodeModelType Expression = new CodeModelType(typeof(Expression));
        public static CodeModelType Assignment = new CodeModelType(Expression, typeof(Assignment));
        public static CodeModelType ChooseField = new CodeModelType(Expression, typeof(ChooseField));
        public static CodeModelType ReadAt = new CodeModelType(Expression, typeof(ReadAt));
        public static CodeModelType NewExpr = new CodeModelType(Expression, typeof(NewExpr));
        public static CodeModelType NullExpr = new CodeModelType(Expression, typeof(NullExpr));
        public static CodeModelType IntLiteral = new CodeModelType(Expression, typeof(IntLiteral));
        public static CodeModelType BoolLiteral = new CodeModelType(Expression, typeof(BoolLiteral));
        public static CodeModelType FloatLiteral = new CodeModelType(Expression, typeof(FloatLiteral));
        public static CodeModelType CharLiteral = new CodeModelType(Expression, typeof(CharLiteral));
        public static CodeModelType StringLiteral = new CodeModelType(Expression, typeof(StringLiteral));
        public static CodeModelType Name = new CodeModelType(Expression, typeof(Name));
        public static CodeModelType FunCall = new CodeModelType(Expression, typeof(FunCall));
        public static CodeModelType UnaryOperation = new CodeModelType(Expression, typeof(UnaryOperation));
        public static CodeModelType BinaryOperation = new CodeModelType(Expression, typeof(BinaryOperation));
        public static CodeModelType FunExpr = new CodeModelType(Expression, typeof(FunExpr));
        public static CodeModelType PostIncExpr = new CodeModelType(Expression, typeof(PostIncExpr));
        public static CodeModelType SelectExpr = new CodeModelType(Expression, typeof(SelectExpr));
        public static CodeModelType MapExpr = new CodeModelType(Expression, typeof(MapExpr));
        public static CodeModelType AccumulateExpr = new CodeModelType(Expression, typeof(AccumulateExpr));
        public static CodeModelType ReduceExpr = new CodeModelType(Expression, typeof(ReduceExpr));
        public static CodeModelType TupleExpr = new CodeModelType(Expression, typeof(TupleExpr));
        public static CodeModelType TableExpr = new CodeModelType(Expression, typeof(TableExpr));
        public static CodeModelType RecordExpr = new CodeModelType(Expression, typeof(RecordExpr));
        public static CodeModelType ParanthesizedExpr = new CodeModelType(Expression, typeof(ParanthesizedExpr));

        static SortedDictionary<string, HeronType> types = null;

        static void AddType(HeronType prim)
        {
            Debug.Assert(prim != null);
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
                if (!(t is CodeModelType))
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
                if (t is CodeModelType)
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
