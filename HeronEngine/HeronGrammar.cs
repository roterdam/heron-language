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
        #region new rule functions
        public static Rule AnyCharExcept(Rule r)
        {
            return Star(Not(r) + AnyChar);
        }
        
        public static Rule Token(string s) 
        {
            return Token(CharSeq(s));
        }
        public static Rule Token(Rule r) 
        {
            return (r + WS);
        }
        public static Rule Word(string s) 
        {
            return (CharSeq(s) + EOW + WS);
        }
        
        public static Rule CommaList(Rule r)
        {
            return Opt((r + Star(Token(",") + NoFail(r))));
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

        #region non-Heron-specific rules
        public static Rule UntilEndOfLine = WhileNot(AnyChar, NL);
        public static Rule LineComment = CharSeq("//") + UntilEndOfLine;
        public static Rule CloseFullComment = CharSeq("*/");
        public static Rule BlockComment = (CharSeq("/*") + NoFail(AnyCharExcept(CloseFullComment) + CloseFullComment));
        public static Rule Comment = BlockComment | LineComment;
        public static Rule WS = Star(CharSet(" \t\n\r") | Comment);
        public static Rule Symbol = CharSet(",") | Plus(CharSet(".~`!@#$%^&*-+|:<>=?/"));
        public static Rule IntegerLiteral = Store("int", Opt(SingleChar('-')) + Plus(Digit));
        public static Rule EscapeChar = SingleChar('\\') + AnyChar;
        public static Rule StringCharLiteral = EscapeChar | NotChar('"');
        public static Rule CharLiteral = Store("char", SingleChar('\'') + StringCharLiteral + SingleChar('\''));
        public static Rule StringLiteral = Store("string", (SingleChar('\"') + Star(StringCharLiteral) + SingleChar('\"')));
        public static Rule FloatLiteral = Store("float", Opt(SingleChar('-')) + Plus(Digit) + SingleChar('.') + Plus(Digit));
        public static Rule HexValue = Store("hex", Plus(HexDigit));
        public static Rule HexLiteral = CharSeq("0x") + NoFail(HexValue);
        public static Rule BinaryValue = Store("bin", Plus(BinaryDigit));
        public static Rule BinaryLiteral = CharSeq("0b") + NoFail(BinaryValue);
        public static Rule NumLiteral = HexLiteral | BinaryLiteral | FloatLiteral | IntegerLiteral;
        public static Rule Literal = (StringLiteral | CharLiteral | NumLiteral) + WS;
        #endregion

        #region expression rules
        public static Rule SpecialDelimiter = Token("forall");
        public static Rule SpecialName = Token(Store("specialname", CharSeq("null") | CharSeq("true") | CharSeq("false")));
        public static Rule Name = Token(Store("name", Not(SpecialDelimiter) + (Symbol | Ident)));
        public static Rule TypeArgs = Store("typeargs", (CharSeq("<") + NoFail(Delay(() => TypeExpr) + Token(">"))));
        public static Rule TypeName = Store("name", (Ident + Star((Token(".") + NoFail(Ident)))));
	    public static Rule TypeExpr = Store("type", (TypeName + Opt(TypeArgs) + WS));
	    public static Rule TypeDecl = Token(":") + NoFail(TypeExpr);
        public static Rule Arg = Store("arg", Name + Opt(TypeDecl));
        public static Rule ArgList = Store("arglist", (Token("(") + CommaList(Arg) + NoFail(Token(")"))));
        public static Rule DelayedStatement = Delay(() => Statement);
        public static Rule CodeBlock = Store("codeblock", BracedGroup(DelayedStatement));
        public static Rule AnonFxn = Store("anonfxn", Token("function") + NoFail(ArgList + Opt(TypeDecl) + CodeBlock));
        public static Rule ParanthesizedExpr = Store("paranexpr", Paranthesized(Opt(Delay(() => Expr))));
        public static Rule BracketedExpr = Store("bracketedexpr", Bracketed(Opt(Delay(() => Expr))));
        public static Rule NewExpr = Store("new", Token("new") + NoFail(TypeExpr + ParanthesizedExpr));
        public static Rule SelectExpr = Store("select", Token("select") + NoFail(Token("(") + Name + Token("from") + Delay(() => Expr) + Token(")") + Delay(() => Expr)));
        public static Rule AccumulateExpr = Store("accumulate", Token("accumulate") + NoFail(Token("(") + Name + Delay(() => Initializer) + Token("forall") + Name + Token("in") + Delay(() => Expr) + Token(")") + Delay(() => Expr)));
        public static Rule MapEachExpr = Store("mapeach", Token("mapeach") + NoFail(Token("(") + Name + Token("in") + Delay(() => Expr) + Token(")") + NoFail(Delay(() => Expr))));
        public static Rule BasicExpr = (NewExpr | MapEachExpr | SelectExpr | AccumulateExpr | AnonFxn | SpecialName | Name | Literal | ParanthesizedExpr | BracketedExpr);
        public static Rule Expr = Store("expr", Plus(BasicExpr));
        #endregion

        #region statement related rules
        public static Rule Eos = Token(";");
        public static Rule Initializer = (Token("=") + NoFail(Expr));
        public static Rule DeleteStatement = Store("delete", Token("delete") + NoFail(Expr + Eos));
        public static Rule VarDecl = Store("vardecl", Token("var") + NoFail(Name + Opt(TypeDecl) + Opt(Initializer) + Eos));
	    public static Rule ElseStatement = (Token("else") + NoFail(DelayedStatement));
        public static Rule IfStatement= Store("if", Token("if") + NoFail(ParanthesizedExpr + DelayedStatement + Opt(ElseStatement)));
        public static Rule ForEachParams = NoFail(Token("(") + Name + Opt(TypeDecl) + Token("in") + Expr + Token(")"));
	    public static Rule ForEachStatement = Store("foreach", Token("foreach") + NoFail(ForEachParams + DelayedStatement));
        public static Rule ForParams = NoFail(Token("(") + Name + Initializer + Eos + Expr + Eos + Expr + Token(")"));
        public static Rule ForStatement = Store("for", Token("for") + NoFail(ForParams + DelayedStatement));
	    public static Rule ExprStatement = Store("exprstatement", Expr + Eos);
        public static Rule ReturnStatement = Store("return", Token("return") + NoFail(Expr + Eos));
	    public static Rule CaseStatement = Store("case", Token("case") + NoFail(ParanthesizedExpr + CodeBlock));
	    public static Rule DefaultStatement = Store("default", (Token("default") + NoFail(CodeBlock)));
        public static Rule CaseGroup = Store("casegroup", Star(CaseStatement));
        public static Rule SwitchStatement = Store("switch", Token("switch") + NoFail(ParanthesizedExpr + Token("{") + CaseGroup + Opt(DefaultStatement) + Token("}")));
	    public static Rule WhileStatement = Store("while", Token("while") + NoFail(ParanthesizedExpr + DelayedStatement));
	    public static Rule EmptyStatement = Store("empty", Eos);
	    public static Rule Statement = (CodeBlock | VarDecl | IfStatement | SwitchStatement | ForEachStatement | ForStatement | WhileStatement | ReturnStatement | DeleteStatement | ExprStatement | EmptyStatement);
        #endregion

        #region structural rules
        public static Rule Field = Store("attribute", (Name + NoFail(Opt(TypeDecl) + Eos)));
        public static Rule FunDecl = Store("fundecl", Name + ArgList + Opt(TypeDecl));
        public static Rule EOSOrCodeBlock = Eos | CodeBlock;
        public static Rule Method = Store("method", FunDecl + NoFail(EOSOrCodeBlock));
        public static Rule Annotation = Store("annotation", Ident + Opt(Initializer));
        public static Rule Annotations = Store("annotations", Token("[") + NoFail(CommaList(Annotation) + Token("]")));
        public static Rule TypeExprList = Token("{") + Star(TypeExpr + NoFail(Eos)) + NoFail(Token("}"));
        public static Rule Implements = Store("implements", Token("implements") + NoFail(TypeExprList));
        public static Rule Inherits = Store("inherits", Token("inherits") + NoFail(BracedGroup(TypeExpr + NoFail(Eos))));
        public static Rule Fields = Store("fields", Token("fields") + NoFail(BracedGroup(Field)));
        public static Rule Methods = Store("methods", Token("methods") + NoFail(BracedGroup(Method)));
        public static Rule Import = Store("import", Name + NoFail(Eos));
        public static Rule Imports = Store("imports", Token("imports") + NoFail(BracedGroup(Import)));
        public static Rule ClassBody = NoFail(Token("{") + Opt(Inherits) + Opt(Implements) + Opt(Fields) + Opt(Methods) + Token("}"));
        public static Rule Class = Store("class", Opt(Annotations) + Token("class") + NoFail(Name) + ClassBody);
        public static Rule Interface = Store("interface", Opt(Annotations) + Token("interface") + NoFail(Name + Token("{") + Opt(Inherits) + Opt(Methods) + Token("}")));
        public static Rule EnumValue = Name + Eos;
        public static Rule EnumValues = Store("values", BracedGroup(EnumValue));
        public static Rule Enum = Store("enum", Opt(Annotations) + Token("enum") + NoFail(Name + EnumValues));
        public static Rule ModuleElement = Class | Interface | Enum;
        public static Rule Module = Store("module", Token("module") + NoFail(Name + BracedGroup(ModuleElement)));
        #endregion

        static HeronGrammar()
        {
            AssignRuleNames(typeof(HeronGrammar));
        }
    }
}
