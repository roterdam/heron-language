/// Heron language interpreter for Windows in C#
/// http://www.heron-language.com
/// Copyright (c) 2009 Christopher Diggins
/// Licenced under the MIT License 1.0 
/// http://www.opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Peg;

namespace HeronEngine
{
    public class HeronGrammar 
        : Grammar
    {
        #region C++ tokenizing rules
        public static Rule LineExt = CharSeq("\\") + EndOfLine;
        public static Rule AnyCharExceptEndOfLine = LineExt | AnyCharExcept(EndOfLine);
        public static Rule UntilEndOfLine = Star(AnyCharExceptEndOfLine);
        public static Rule UntilPastEndOfLine = UntilEndOfLine + EndOfLine;
        public static Rule LineComment = CharSeq("//") + UntilEndOfLine;
        public static Rule BlockComment = CharSeq("/*") + AllCharsExcept(CharSeq("*/")) + CharSeq("*/");
        public static Rule Comment = BlockComment | LineComment;
        public static Rule SimpleWS = CharSet(" \t") | EndOfLine;
        public static Rule SingleLineWS = Star(CharSet(" \t") | Comment);
        public static Rule WS = Star(SimpleWS | Comment);
        #endregion

        #region new rule functions
        public static Rule Token(string s)
        {
            return Token(CharSeq(s));
        }
        public static Rule Token(Rule r)
        {
            return r + WS;
        }
        public static Rule Word(string s)
        {
            return CharSeq(s) + EOW + WS;
        }
        public static Rule CommaList(Rule r)
        {
            return Opt(r + Star(Token(",") + NoFail(r)));
        }

        public static Rule SemiColonList(Rule r)
        {
            return Opt(r + Star(Token(";") + NoFail(r)));
        }

        public static Rule Paranthesized(Rule r)
        {
            return (Token("(") + NoFail(r + Token(")")));
        }

        public static Rule Bracketed(Rule r)
        {
            return (Token("[") + NoFail(r + Token("]")));
        }

        public static Rule BracedGroup(Rule r)
        {
            return (Token("{") + Star(r) + NoFail(Token("}")));
        }
        #endregion

        #region Heron-specific tokenizing rules
        public static Rule Symbol = Plus(CharSet(".~`!#$%^&*-+|:<>=?/"));
        public static Rule IntegerLiteral = Store("int", Opt(SingleChar('-')) + Plus(Digit));
        public static Rule EscapeChar = SingleChar('\\') + AnyChar;
        public static Rule StringCharLiteral = EscapeChar | (Not(CharSet("\n\"")) + AnyChar);
        public static Rule VerbStringCharLiteral = CharSeq("\"\"") | NotChar('"');
        public static Rule CharLiteral = Store("char", SingleChar('\'') + StringCharLiteral + SingleChar('\''));
        public static Rule StringLiteral = Store("string", (SingleChar('\"') + Star(StringCharLiteral) + SingleChar('\"')));
        public static Rule VerbStringLiteral = Store("verbstring", (CharSeq("@\"") + NoFail(Star(VerbStringCharLiteral) + SingleChar('\"'))));
        public static Rule FloatLiteral = Store("float", Opt(SingleChar('-')) + Plus(Digit) + SingleChar('.') + Plus(Digit));
        public static Rule HexValue = Store("hex", Plus(HexDigit));
        public static Rule HexLiteral = CharSeq("0x") + NoFail(HexValue);
        public static Rule BinaryValue = Store("bin", Plus(BinaryDigit));
        public static Rule BinaryLiteral = CharSeq("0b") + NoFail(BinaryValue);
        public static Rule NumLiteral = HexLiteral | BinaryLiteral | FloatLiteral | IntegerLiteral;
        public static Rule Literal = (VerbStringLiteral | StringLiteral | CharLiteral | NumLiteral) + WS;
        public static Rule Eos = Token(";");
        #endregion

        #region type expression rules
        public static Rule TypeArgs = Store("typeargs", (Token("<") + NoFail(Delay("typeexpr", () => TypeExpr) + Token(">"))));
        public static Rule TypeName = Store("name", (Ident + Star(Token(".") + NoFail(Ident)))) + WS;
        public static Rule Nullable = Store("nullable", CharSeq("?")) + WS;
        public static Rule TypeExpr = Store("typeexpr", (TypeName + WS + Opt(TypeArgs)));
        public static Rule TypeDecl = Store("typedecl", (Token(":") + NoFail(TypeExpr) + Opt(Nullable)));
        public static Rule TypeExprList = Token("{") + Star(TypeExpr + NoFail(Eos)) + NoFail(Token("}"));
        #endregion 

        #region expression rules
        public static Rule DelayedBasicExpr = Delay("basicexpr", () => BasicExpr);
        public static Rule DelayedExpr = Delay("expr", () => CompoundExpr);
        public static Rule SpecialDelimiter = Word("forall") + WS;
        public static Rule SpecialName = Store("specialname", CharSeq("null") | CharSeq("true") | CharSeq("false")) + EOW + WS;
        public static Rule Name = Store("name", Not(SpecialDelimiter) + Not(SpecialName) + (Symbol | Ident)) + WS;
        public static Rule Param = Store("arg", Name + Opt(TypeDecl));
        public static Rule ArgList = Store("arglist", (Token("(") + CommaList(Param) + NoFail(Token(")"))));
        public static Rule DelayedStatement = Delay("Statement", () => Statement);
        public static Rule CodeBlock = Store("codeblock", BracedGroup(DelayedStatement));
        public static Rule FunExpr = Store("funexpr", Word("function") + NoFail(ArgList + Opt(TypeDecl) + CodeBlock));
        public static Rule ParanthesizedExpr = Store("paranexpr", Paranthesized(CommaList(DelayedExpr)));
        public static Rule BracketedExpr = Store("bracketedexpr", Bracketed(CommaList(DelayedExpr)));
        public static Rule NewExpr = Store("new", Word("new") + NoFail(TypeExpr + ParanthesizedExpr) + Opt(Word("from") + Name));
        public static Rule SelectExpr = Store("select", Word("select") + NoFail(Token("(") + Name + Word("from") + DelayedExpr + Token(")") + DelayedExpr));
        public static Rule AccumulateExpr = Store("accumulate", Word("accumulate") + NoFail(Token("(") + Name + Delay("Initializer", () => Initializer) + Word("forall") + Name + Word("in") + DelayedExpr + Token(")") + DelayedExpr));
        public static Rule ReduceExpr = Store("reduce", Word("reduce") + NoFail(Token("(") + Name + Token(",") + Name + Word("in") + DelayedExpr + Token(")") + NoFail(DelayedExpr)));
        public static Rule MapExpr = Store("map", Word("map") + NoFail(Token("(") + Name + Word("in") + DelayedExpr + Token(")") + NoFail(DelayedExpr)));
        public static Rule Row = Store("row", CommaList(DelayedExpr) + Eos);
        public static Rule Rows = Store("rows", NoFail(BracedGroup(Row)));
        public static Rule RecordFields = Store("recordfields", CommaList(DelayedExpr));
        public static Rule RecordExpr = Store("record", Word("record") + NoFail(ArgList) + NoFail(Token("{")) + NoFail(RecordFields) + NoFail(Token("}")));
        public static Rule TableExpr = Store("table", Word("table") + NoFail(ArgList) + NoFail(Rows));
        public static Rule BasicExpr = (NewExpr | MapExpr | SelectExpr | AccumulateExpr | ReduceExpr | FunExpr | TableExpr | RecordExpr | SpecialName | Name | Literal | ParanthesizedExpr | BracketedExpr);
        public static Rule CompoundExpr = Store("expr", Plus(BasicExpr));
        #endregion

        #region meta-information rules
        public static Rule Annotations = Store("annotations", Token("[") + NoFail(CommaList(CompoundExpr) + Token("]")));
        #endregion structural rules

        #region statement related rules
        public static Rule Initializer = (Token("=") + NoFail(CompoundExpr));
        public static Rule DeleteStatement = Store("delete", Word("delete") + NoFail(CompoundExpr + Eos));
        public static Rule VarDecl = Store("vardecl", Opt(Annotations) + Word("var") + NoFail(Name + Opt(TypeDecl) + Opt(Initializer) + Eos));
        public static Rule ElseStatement = (Word("else") + NoFail(DelayedStatement));
        public static Rule IfStatement = Store("if", Word("if") + NoFail(ParanthesizedExpr + DelayedStatement + Opt(ElseStatement)));
        public static Rule ForEachParams = NoFail(Token("(") + Name + Opt(TypeDecl) + Word("in") + CompoundExpr + Token(")"));
        public static Rule ForEachStatement = Store("foreach", Word("foreach") + NoFail(ForEachParams + DelayedStatement));
        public static Rule ForParams = NoFail(Token("(") + Name + Initializer + Eos + CompoundExpr + Eos + CompoundExpr + Token(")"));
        public static Rule ForStatement = Store("for", Word("for") + NoFail(ForParams + DelayedStatement));
        public static Rule ExprStatement = Store("exprstatement", CompoundExpr + Eos);
        public static Rule ReturnStatement = Store("return", Word("return") + NoFail(Opt(CompoundExpr) + Eos));
        public static Rule CaseStatement = Store("case", Word("case") + NoFail(ParanthesizedExpr + CodeBlock));
        public static Rule DefaultStatement = Store("default", (Word("default") + NoFail(CodeBlock)));
        public static Rule CaseGroup = Store("casegroup", Star(CaseStatement));
        public static Rule SwitchStatement = Store("switch", Word("switch") + NoFail(ParanthesizedExpr + Token("{") + CaseGroup + Opt(DefaultStatement) + Token("}")));
        public static Rule WhileStatement = Store("while", Word("while") + NoFail(ParanthesizedExpr + DelayedStatement));
        public static Rule EmptyStatement = Store("empty", Eos);
        public static Rule Statement = (CodeBlock | VarDecl | IfStatement | SwitchStatement | ForEachStatement | ForStatement | WhileStatement | ReturnStatement | DeleteStatement | ExprStatement | EmptyStatement);
        #endregion

        #region structural rules
        public static Rule Field = Store("field", Opt(Annotations) + Name + Opt(TypeDecl) + Opt(Initializer) + Eos);
        public static Rule FunDecl = Store("fundecl", Name + ArgList + Opt(TypeDecl));
        public static Rule EOSOrCodeBlock = Eos | CodeBlock;
        public static Rule EmptyMethod = Store("method", FunDecl + NoFail(Eos));
        public static Rule Method = Store("method", Opt(Annotations) + FunDecl + NoFail(CodeBlock));
        public static Rule Implements = Store("implements", Word("implements") + NoFail(TypeExprList));
        public static Rule Inherits = Store("inherits", Word("inherits") + NoFail(BracedGroup(TypeExpr + NoFail(Eos))));
        public static Rule Fields = Store("fields", Word("fields") + NoFail(BracedGroup(Field)));
        public static Rule Methods = Store("methods", Word("methods") + NoFail(BracedGroup(Method)));
        public static Rule EmptyMethods = Store("methods", Word("methods") + NoFail(BracedGroup(EmptyMethod)));
        public static Rule Import = Store("import", Name + NoFail(Token("=")) + NoFail(Word("new")) + NoFail(TypeName) + NoFail(ParanthesizedExpr) + NoFail(Eos));
        public static Rule Imports = Store("imports", Word("imports") + NoFail(BracedGroup(Import)));
        public static Rule ClassBody = NoFail(Token("{") + Opt(Inherits) + Opt(Implements) + Opt(Fields) + Opt(Methods) + Token("}"));
        public static Rule Class = Store("class", Opt(Annotations) + Word("class") + NoFail(Name) + ClassBody);
        public static Rule Interface = Store("interface", Opt(Annotations) + Word("interface") + NoFail(Name + Token("{") + Opt(Inherits) + Opt(EmptyMethods) + Token("}")));
        public static Rule EnumValue = Name + Eos;
        public static Rule EnumValues = Store("values", BracedGroup(EnumValue));
        public static Rule Enum = Store("enum", Opt(Annotations) + Word("enum") + NoFail(Name + EnumValues));
        public static Rule TypeDefinition = Class | Interface | Enum;
        public static Rule ModuleBody = Store("modulebody", NoFail(Token("{") + Opt(Imports) + Opt(Fields) + Opt(Methods) + Token("}")));
        public static Rule Module = Store("module", Opt(Annotations) + Word("module") + NoFail(TypeName) + NoFail(ModuleBody) + Star(TypeDefinition));
        public static Rule File = NoFail(Module) + NoFail(EndOfInput);
        #endregion

        static HeronGrammar()
        {
            AssignRuleNames(typeof(HeronGrammar));
        }
    }
}
