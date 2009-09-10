/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;
using System.Collections.Generic;
using System.Text;
using Peg;

namespace HeronEngine
{
    public class HeronGrammar 
        : Grammar
    {
        #region non-Heron-specific rules
        public static Rule AnyCharExcept(Rule r)
        {
            return Star(Not(r) + AnyChar());
        }

        public static Rule UntilEndOfLine()
        {
            return WhileNot(AnyChar(), NL()).SetName("until end of line");
        }
        public static Rule LineComment() 
        {
            return (CharSeq("//") + UntilEndOfLine()).SetName("line comment"); 
        }
        public static Rule CloseFullComment()
        {
            return CharSeq("*/").SetName("end comment");
        }
        public static Rule BlockComment()
        {
            return (CharSeq("/*") + NoFail(AnyCharExcept(CloseFullComment()) + CloseFullComment())).SetName("block comment");
        }
        public static Rule Comment() 
        {
            return (BlockComment() | LineComment()).SetName("comment");
        }
        public static Rule WS() 
        {
            return Star(CharSet(" \t\n\r") | Comment()).SetName("white space");
        }
        public static Rule Symbol()
        {
            return (CharSet(",.") | Plus(CharSet("~`!@#$%^&*-+|:<>=?/"))).SetName("symbol");
        }
        public static Rule Token(string s) 
        {
            return Token(CharSeq(s)).SetName("'" + s + "'");
        }
        public static Rule Token(Rule r) 
        {
            return (r + WS()).SetName(r.GetName());
        }
        public static Rule Word(string s) 
        {
            return (CharSeq(s) + EOW() + WS()).SetName("word");
        }
        public static Rule IntegerLiteral() 
        {
            return Store("int", Opt(SingleChar('-')) + Plus(Digit())).SetName("integer literal");
        }
        public static Rule EscapeChar() 
        {
            return (SingleChar('\\') + AnyChar()).SetName("escape character");
        }
        public static Rule StringCharLiteral() 
        {
            return (EscapeChar() | NotChar('"')).SetName("string char literal");
        }
        public static Rule CharLiteral() 
        {
            return Store("char", SingleChar('\'') + StringCharLiteral() + SingleChar('\'')).SetName("char literal");
        }
        public static Rule StringLiteral() 
        {
            return Store("string", (SingleChar('\"') + Star(StringCharLiteral()) + SingleChar('\"'))).SetName("string literal");
        }
        public static Rule FloatLiteral() 
        {
            return Store("float", Opt(SingleChar('-')) + Plus(Digit()) + SingleChar('.') + Plus(Digit())).SetName("float literal");
        }
        public static Rule HexValue()
        {
            return Store("hex", Plus(HexDigit())).SetName("hexadecimal value");
        }
        public static Rule HexLiteral()
        {
            return (CharSeq("0x") + NoFail(HexValue())).SetName("hexadecimal literal");
        }
        public static Rule BinaryValue()
        {
            return Store("bin", Plus(BinaryDigit())).SetName("binary value");
        }
        public static Rule BinaryLiteral()
        {
            return (CharSeq("0b") + NoFail(BinaryValue())).SetName("binary literal");
        }
        public static Rule NumLiteral()
        {
            return (HexLiteral() | BinaryLiteral() | FloatLiteral() | IntegerLiteral()).SetName("numeric literal");
        }
        public static Rule Literal() 
        {
            return ((StringLiteral() | CharLiteral() | NumLiteral()) + WS()).SetName("literal");
        }        
        public static Rule Name() 
        {
            return Token(Store("name", (Symbol() | Ident()))).SetName("name");
        }
        public static Rule CommaList(Rule r)
        {
            return Opt((r + Star(Token(",") + NoFail(r)))).SetName("comma delimited list");
        }
        #endregion

        #region Heron-specific rules
        public static Rule TypeArgs() 
        {
            return Store("typeargs", (CharSeq("<") + NoFail(Delay(TypeExpr) + Token(">")))).SetName("type arguments");
        }

        public static Rule NameOrLiteral()
        {
            // The order here is important, otherwise "-3" gets parsed as a name ('-')
            // followed by a literal ('3')
            return (Literal() | Name()).SetName("literal or name");
        }

        /// <summary>
        /// A type name could be an ordinary
        /// </summary>
        /// <returns></returns>
        public static Rule TypeName()
        {
            return Store("name", (Ident() + Star((Token(".") + NoFail(Ident()))))).SetName("type name");
        }

	    public static Rule TypeExpr() 
        {
            return Store("type", (TypeName() + Opt(TypeArgs()) + WS())).SetName("type expression");
        }

	    public static Rule TypeDecl()
        {
            return (Token(":") + NoFail(TypeExpr())).SetName("type declaration");
        }
       
        public static Rule Arg() 
        {
            return Store("arg", Name() + Opt(TypeDecl())).SetName("argument");
        }
        
        public static Rule ArgList() 
        {
            return Store("arglist", (Token("(") + CommaList(Arg()) + NoFail(Token(")")))).SetName("argument list");
        }

        public static Rule AnonFxn() 
        {
            return Store("anonfxn", Token("function") + NoFail(ArgList() + Opt(TypeDecl()) + CodeBlock())).SetName("anonymous function");
        }

        public static Rule DeleteStatement()
        {
            return Store("delete", Token("delete") + NoFail(Expr() + Eos())).SetName("delete statement");
     }

        public static Rule Paranthesized(Rule r)
        {
            return (Token("(") + NoFail(r + Token(")"))).SetName("paranthesized rule");
        }

        public static Rule ParanthesizedExpr() 
        {
            return Store("paranexpr", Paranthesized(Opt(Delay(Expr)))).SetName("paranthesized expression");
        }

        public static Rule Bracketed(Rule r)
        {
            return (Token("[") + NoFail(r + Token("]"))).SetName("bracketed rule");
        }

        public static Rule BracketedExpr()
        {
            return Store("bracketedexpr", Bracketed(Opt(Delay(Expr)))).SetName("bracketed expression");
        }

        public static Rule NewExpr()
        {
            return Store("new", Token("new") + NoFail(TypeExpr() + ParanthesizedExpr())).SetName("new expression");
        }

        public static Rule SimpleExpr() 
        {
            return (NewExpr() | Name() | Literal() |
                AnonFxn() | ParanthesizedExpr() | BracketedExpr()).SetName("simple expression");
		}

        public static Rule Expr() 
        {
            return Store("expr", Plus(SimpleExpr())).SetName("expression");
        }
        
        public static Rule Initializer() 
        {
            return (Token("=") + NoFail(Expr())).SetName("initializer");
        }
        
        public static Rule VarDecl()
        {
            return Store("vardecl", Token("var") + NoFail(Name() + Opt(TypeDecl()) + Opt(Initializer()) + Eos())).SetName("variable declaration");
        }
        public static Rule DelayedStatement()
        {
            return Delay(Statement).SetName("statement");
        }
	    public static Rule ElseStatement()
        {
            return (Token("else") + NoFail(DelayedStatement())).SetName("else statement");
        }
        public static Rule IfStatement()
        {
            return Store("if", Token("if") + NoFail(ParanthesizedExpr() + DelayedStatement() + Opt(ElseStatement()))).SetName("if statement");
        }
        public static Rule ForEachParms()
        {
            return NoFail(Token("(") + Name() + Opt(TypeDecl()) + Token("in") + Expr() + CharSeq(")")).SetName("foreach statement parameters");
        }
	    public static Rule ForEachStatement()
        {
            return Token("foreach") + NoFail(ForEachParms() + DelayedStatement()).SetName("foreach statement");
        }
        public static Rule ForParams()
        {
            return NoFail(Token("(") + Name() + Initializer() + Eos() + Expr() + Eos() + Expr() + Token(")")).SetName("for statement parameters");
        }
        public static Rule ForStatement()
        {
            return (Token("for") + NoFail(ForParams() + DelayedStatement())).SetName("for statement");
        }
        public static Rule Eos()
        {
            return Token(";").SetName("end of statement");
        }
	    public static Rule ExprStatement()
        {
            return Store("exprstatement", Expr() + Eos()).SetName("expression statement");
        }
        public static Rule ReturnStatement()
        {
            return (Token("return") + NoFail(Expr() + Eos())).SetName("return statement");
        }
	    public static Rule CaseStatement()
        {
            return (Token("case") + NoFail(ParanthesizedExpr() + CodeBlock())).SetName("case statement");
        }
	    public static Rule DefaultStatement()
        {
            return Store("default", (Token("default") + NoFail(CodeBlock()))).SetName("default statement");
        }
        public static Rule CaseGroup()
        {
            return Store("casegroup", Star(CaseStatement())).SetName("case group");
        }
        public static Rule SwitchStatement()
        {
            return Store("switch", Token("switch") + NoFail(ParanthesizedExpr() + Token("{") +
                CaseGroup() + Opt(DefaultStatement()) + Token("}"))).SetName("switch statement");
        }
	    public static Rule WhileStatement()
        {
            return Store("while", Token("while") + NoFail(ParanthesizedExpr() + DelayedStatement())).SetName("while statement");
        }
	    public static Rule EmptyStatement()
        {
            return Eos().SetName("empty statement");
        }
	    public static Rule Statement()
        {
            return (CodeBlock() | VarDecl() | IfStatement() | SwitchStatement() | ForEachStatement() | ForStatement()
                | WhileStatement() | ReturnStatement() | DeleteStatement() | ExprStatement() | EmptyStatement()).SetName("statement");
        }
        public static Rule BracedGroup(Rule r)
        {
            return Token("{") + Star(Token("}") | NoFail(r));
        }
        public static Rule Field()
        {
            return Store("attribute", (Name() + NoFail(Opt(TypeDecl()) + Eos()))).SetName("field");
        }
        public static Rule CodeBlock()
        {
            return Store("codeblock", BracedGroup(DelayedStatement())).SetName("code block");
        }
        public static Rule FunDecl()
        {
            return Store("fundecl", Name() + ArgList() + Opt(TypeDecl())).SetName("function declaration");
        }
        public static Rule EOSOrCodeBlock()
        {
            return (Eos() | CodeBlock()).SetName("';' or code block");
        }
        public static Rule Method()
        {
            return Store("method", FunDecl() + NoFail(EOSOrCodeBlock())).SetName("method");
        }
        public static Rule Entry()
        {
            return Store("entry", (Token("entry") + CodeBlock())).SetName("entry");
        }
        public static Rule Transition()
        {
            return Store("transition", TypeExpr() + NoFail(Token("->") + Name() + Eos())).SetName("transition");
        }
        public static Rule Transitions()
        {
            return Store("transitions", Token("transitions") + NoFail(BracedGroup(Transition()))).SetName("transitions");
        }
        public static Rule State()
        {
            return Store("state", Name() + NoFail(ArgList() + Token("{") + Opt(Entry()) + Opt(Transitions()) + Token("}"))).SetName("state");
        }
        public static Rule States()
        {
            return Store("states", Token("states") + NoFail(BracedGroup(State()))).SetName("states");
        }
        public static Rule Annotation()
        {
            return Store("annotation", Ident() + Opt(Initializer())).SetName("annotation");
        }
        public static Rule Annotations()
        {
            return Store("annotations", Token("[") + NoFail(CommaList(Annotation()) + Token("]"))).SetName("annotations");
        }
        public static Rule TypeExprList()
        {
            return (Token("{") + Star(TypeExpr() + NoFail(Eos()) + NoFail(Token("}")))).SetName("type expression list");
        }
        public static Rule Implements()
        {
            return Store("implements", Token("implements") + NoFail(TypeExprList())).SetName("implements");
        }
        public static Rule Inherits()
        {
            return Store("inherits", Token("inherits") + NoFail(BracedGroup(TypeExpr() + NoFail(Eos())))).SetName("inherits");
        }
        public static Rule ClassBody()
        {
            return NoFail(Token("{") + Opt(Inherits()) + Opt(Implements()) + Opt(Methods()) + Opt(Fields()) + Opt(States())).SetName("class definition");
        }
        public static Rule Class()
        {
            return Store("class", Opt(Annotations()) + Token("class") + NoFail(Name()) + ClassBody()).SetName("class");
        }
        public static Rule Interface()
        {
            return Store("interface", Opt(Annotations()) + Token("interface")
                + NoFail(Name() + Token("{") + Opt(Inherits()) + Opt(Methods()) + Token("}"))).SetName("interface");
        }
        public static Rule EnumValue()
        {
            return (Name() + Eos()).SetName("enumerated value");
        }
        public static Rule EnumValues()
        {
            return Store("values", BracedGroup(EnumValue())).SetName("enumerated value group");
        }
        public static Rule Enum()
        {
            return Store("enum", Opt(Annotations()) + Token("enum") + NoFail(Name() + EnumValues())).SetName("enumeration");
        }
        public static Rule ModuleElement()
        {
            return (Class() | Interface() | Enum()).SetName("module element");
        }
        public static Rule Fields()
        {
            return Store("fields", Token("fields") + NoFail(BracedGroup(Field()))).SetName("fields");
        }
        public static Rule Methods()
        {
            return Store("methods", Token("methods") + NoFail(BracedGroup(Method()))).SetName("methods");
        }
       public static Rule Import()
        {
            return Store("import", Name() + NoFail(Eos())).SetName("import");
        }
        public static Rule Imports()
        {
            return (Store("imports", Token("imports") + NoFail(BracedGroup(Import())))).SetName("imports");
        }
        public static Rule Module()
        {
            return (Token("module") + NoFail(Name() + BracedGroup(ModuleElement()))).SetName("module");
        }
        #endregion
    }
}
