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

        public static Rule StoreSeqIf(string s, Rule[] x) { return Seq(Token(s), Store(s, NoFailSeq(x))); }
        public static Rule StoreSeqIf(string s, Rule x0, Rule x1) { return StoreSeqIf(s, new Rule[] { x0, x1, } ); }
        public static Rule StoreSeqIf(string s, Rule x0, Rule x1, Rule x2) { return StoreSeqIf(s, new Rule[] { x0, x1, x2 }); }
        public static Rule StoreSeqIf(string s, Rule x0, Rule x1, Rule x2, Rule x3) { return StoreSeqIf(s, new Rule[] { x0, x1, x2, x3, }); }
        public static Rule StoreSeqIf(string s, Rule x0, Rule x1, Rule x2, Rule x3, Rule x4) { return StoreSeqIf(s, new Rule[] { x0, x1, x2, x3, x4, }); }
        public static Rule StoreSeqIf(string s, Rule x0, Rule x1, Rule x2, Rule x3, Rule x4, Rule x5) { return StoreSeqIf(s, new Rule[] { x0, x1, x2, x3, x4, x5, }); }
        public static Rule StoreSeqIf(string s, Rule x0, Rule x1, Rule x2, Rule x3, Rule x4, Rule x5, Rule x6) { return StoreSeqIf(s, new Rule[] { x0, x1, x2, x3, x4, x5, x6, }); }
        public static Rule StoreSeqIf(string s, Rule x0, Rule x1, Rule x2, Rule x3, Rule x4, Rule x5, Rule x6, Rule x7) { return StoreSeqIf(s, new Rule[] { x0, x1, x2, x3, x4, x5, x6, x7 }); }

        public static Rule StoreNoFailSeqIf(string sNodeName, string sToken, Rule[] rs)
        {
            return Seq(Token(sToken), Store(sNodeName, NoFailSeq(rs)));
        }
        
        public static Rule AnyCharExcept(Rule r)
        {
            return Star(Seq(Not(r), AnyChar()));
        }

        public static Rule StoreBracedGroupIf(string s, Rule r)
        {
            return Seq(
                Token(s),
                Store(s, 
                    Seq(new Rule[] { 
                        NoFail(Token("{")),
                        Star(r), 
                        NoFail(Token("}")), })))
                .SetName(s);
        }

        public static Rule UntilEndOfLine()
        {
            return WhileNot(AnyChar(), NL());
        }
        public static Rule LineComment() 
        {
            return Seq(CharSeq("//"), UntilEndOfLine()).SetName("line comment"); 
        }
        public static Rule CloseFullComment()
        {
            return CharSeq("*/").SetName("end comment");
        }
        public static Rule BlockComment()
        {
            return NoFailSeq(CharSeq("/*"), AnyCharExcept(CloseFullComment()), CloseFullComment()).SetName("block comment");
        }
        public static Rule Comment() 
        {
            return Choice(BlockComment(), LineComment()).SetName("comment");
        }
        public static Rule WS() 
        {
            return Star(Choice(CharSet(" \t\n\r"), Comment())).SetName("white space");
        }
        public static Rule Symbol()
        {
            return Choice(CharSet(",."), Plus(CharSet("~`!@#$%^&*-+|:<>=?/"))).SetName("symbol");
        }
        public static Rule Token(string s) 
        {
            return Token(CharSeq(s)).SetName(s);
        }
        public static Rule Token(Rule r) 
        {
            return Seq(r, WS()).SetName(r.GetName());
        }
        public static Rule Word(string s) 
        {
            return Seq(CharSeq(s), EOW(), WS()).SetName("word");
        }
        public static Rule IntegerLiteral() 
        {
            return Store("int", Seq(Opt(SingleChar('-')), Plus(Digit()))).SetName("integer literal");
        }
        public static Rule EscapeChar() 
        {
            return Seq(SingleChar('\\'), AnyChar()).SetName("escape character");
        }
        public static Rule StringCharLiteral() 
        {
            return Choice(EscapeChar(), NotChar('"')).SetName("string char literal");
        }
        public static Rule CharLiteral() 
        {
            return Store("char", Seq(SingleChar('\''), StringCharLiteral(), SingleChar('\''))).SetName("char literal");
        }
        public static Rule StringLiteral() 
        {
            return Store("string", Seq(SingleChar('\"'), Star(StringCharLiteral()), SingleChar('\"'))).SetName("string literal");
        }
        public static Rule FloatLiteral() 
        {
            return Store("float", Seq(Opt(SingleChar('-')), Plus(Digit()), SingleChar('.'), Plus(Digit()))).SetName("float literal");
        }
        public static Rule HexValue()
        {
            return Store("hex", Plus(HexDigit())).SetName("hex value");
        }
        public static Rule HexLiteral()
        {
            return NoFailSeq(CharSeq("0x"), HexValue()).SetName("hex literal");
        }
        public static Rule BinaryValue()
        {
            return Store("bin", Plus(BinaryDigit())).SetName("binary value");
        }
        public static Rule BinaryLiteral()
        {
            return NoFailSeq(CharSeq("0b"), BinaryValue()).SetName("binary literal");
        }
        public static Rule NumLiteral()
        {
            return Choice(HexLiteral(), BinaryLiteral(), FloatLiteral(), IntegerLiteral()).SetName("numeric literal");
        }
        public static Rule Literal() 
        {
            return Seq(Choice(StringLiteral(), CharLiteral(), NumLiteral()), WS()).SetName("literal");
        }        
        public static Rule Name() 
        {
            return Token(Store("name", Choice(Symbol(), Ident()))).SetName("name");
        }
        public static Rule CommaList(Rule r)
        {
            return Opt(Seq(r, Star(Seq(Token(","), NoFail(r))))).SetName("comma delimited list");
        }
        #endregion

        #region Heron-specific rules
        public static Rule TypeArgs() 
        {
            return Store("typeargs", NoFailSeq(CharSeq("<"), Delay(TypeExpr), Token(">"))).SetName("type arguments");
        }

        public static Rule NameOrLiteral()
        {
            // The order here is important, otherwise "-3" gets parsed as a name ('-')
            // followed by a literal ('3')
            return Choice(Literal(), Name()).SetName("literal or name");
        }

        /// <summary>
        /// A type name could be an ordinayr
        /// </summary>
        /// <returns></returns>
        public static Rule TypeName()
        {
            return Store("name", Seq(Ident(), Star(Seq(Token("."), NoFail(Ident()))))).SetName("type name");
        }

	    public static Rule TypeExpr() 
        {
            return Store("type", Seq(TypeName(), Opt(TypeArgs()), WS())).SetName("type expression");
        }

	    public static Rule TypeDecl()
        {
            return NoFailSeq(Token(":"), TypeExpr()).SetName("type declaration");
        }
       
        public static Rule Arg() 
        {
            return Store("arg", Seq(Name(), Opt(TypeDecl()))).SetName("argument");
        }
        
        public static Rule ArgList() 
        {
            return Store("arglist", Seq(Token("("), CommaList(Arg()), NoFail(Token(")")))).SetName("argument list");
        }

        public static Rule AnonFxn() 
        {
            return Store("anonfxn", NoFailSeq(Token("function"), ArgList(), Opt(TypeDecl()), CodeBlock())).SetName("anonymous function");
        }

        public static Rule DeleteStatement()
        {
            return Store("delete", NoFailSeq(Token("delete"), Expr())).SetName("delete statement");
     }

        public static Rule Paranthesized(Rule r)
        {
            return NoFailSeq(Token("("), r, Token(")")).SetName("paranthesized rule");
        }

        public static Rule ParanthesizedExpr() 
        {
            return Store("paranexpr", Paranthesized(Opt(Delay(Expr)))).SetName("paranthesized expression");
        }

        public static Rule Bracketed(Rule r)
        {
            return NoFailSeq(Token("["), r, Token("]")).SetName("bracketed rule");
        }

        public static Rule BracketedExpr()
        {
            return Store("bracketedexpr", Bracketed(Opt(Delay(Expr)))).SetName("bracketed expression");
        }

        public static Rule NewExpr()
        {
            return Store("new", NoFailSeq(Token("new"), TypeExpr(), ParanthesizedExpr())).SetName("new expression");
        }

        public static Rule SimpleExpr() 
        {
            return Choice(NewExpr(), Name(), Literal(),
                AnonFxn(), ParanthesizedExpr(), BracketedExpr()).SetName("simple expression");
		}

        public static Rule Expr() 
        {
            return Store("expr", Plus(SimpleExpr())).SetName("expression");
        }
        
        public static Rule Initializer() 
        {
            return NoFailSeq(Token("="), Expr()).SetName("initializer");
        }
        
        public static Rule VarDecl()
        {
            return Store("vardecl", NoFailSeq(Token("var"), Name(), Opt(TypeDecl()), Opt(Initializer()), Eos())).SetName("variable declaration");
        }

	    public static Rule ElseStatement()
        {
            return NoFailSeq(Token("else"), Delay(Statement)).SetName("else statement");
        }
        public static Rule IfStatement()
        {
            return Store("if", NoFailSeq(Token("if"), ParanthesizedExpr(), Delay(Statement), Opt(ElseStatement()))).SetName("if statement");
        }
        public static Rule ForEachParms()
        {
            return NoFailSeq(CharSeq("("), Seq(Name(), Opt(TypeDecl()), Token("in"), Expr()), CharSeq(")")).SetName("foreach statement parameters");
        }
	    public static Rule ForEachStatement()
        {
            return StoreSeqIf("foreach", ForEachParms(), Delay(Statement)).SetName("foreach statement");
        }
        public static Rule ForParams()
        {
            return NoFailSeq(CharSeq("("), Name(), Initializer(), Eos(), Expr(), Eos(), Expr(), CharSeq(")")).SetName("foreach statement parameters");
        }
        public static Rule ForStatement()
        {
            return StoreSeqIf("for", ForParams(), Delay(Statement)).SetName("for statement");
        }
        public static Rule Eos()
        {
            return Token(";").SetName("end of statement");
        }
	    public static Rule ExprStatement()
        {
            return StoreSeqIf("exprstatement", Expr(), Eos()).SetName("expression statement");
        }
        public static Rule ReturnStatement()
        {
            return StoreSeqIf("return", Expr(), Eos()).SetName("return statement");
        }
	    public static Rule CaseStatement()
        {
            return StoreSeqIf("case", ParanthesizedExpr(), CodeBlock()).SetName("case statement");
        }
	    public static Rule DefaultStatement()
        {
            return Store("default", NoFailSeq(Token("default"), CodeBlock())).SetName("default statement");
        }
        public static Rule CaseGroup()
        {
            return Store("casegroup", Star(CaseStatement())).SetName("case group");
        }
        public static Rule SwitchStatement()
        {
            return Store("switch", NoFailSeq(Token("switch"), ParanthesizedExpr(), Token("{"),
                CaseGroup(), Opt(DefaultStatement()), Token("}"))).SetName("switch statement");
        }
	    public static Rule WhileStatement()
        {
            return Store("while", NoFailSeq(Token("while"), ParanthesizedExpr(), Delay(Statement))).SetName("while statement");
        }
	    public static Rule EmptyStatement()
        {
            return Eos().SetName("empty statement");
        }
	    public static Rule Statement()
        {
            return Choice(
                new Rule[] { CodeBlock(), VarDecl(), IfStatement(), SwitchStatement(), ForEachStatement(), ForStatement(),
                WhileStatement(), ReturnStatement(), DeleteStatement(), ExprStatement(), EmptyStatement() }).SetName("statement");
        }
        public static Rule Braced(Rule r)
        {
            return NoFailSeq(Token("{"), NoFail(r), NoFail(Token("}")));
        }
        public static Rule BracedGroup(Rule r)
        {
            return NoFailSeq(Token("{"), Star(Choice(Token("}"), NoFail(r))));
        }
        public static Rule Field()
        {
            return Store("attribute", NoFailSeq(Name(), Opt(TypeDecl()), Eos())).SetName("field");
        }        
        public static Rule CodeBlock()
        {
            return Store("codeblock", BracedGroup(Delay(Statement).SetName("statement"))).SetName("code block");
        }
        public static Rule FunDecl()
        {
            return Store("fundecl", NoFailSeq(Name(), ArgList(), Opt(TypeDecl()))).SetName("function declaration");
        }
        public static Rule EOSOrCodeBlock()
        {
            return Choice(Eos(), CodeBlock()).SetName("';' or code block");
        }
        public static Rule Method()
        {
            return Store("method", NoFailSeq(FunDecl(), EOSOrCodeBlock())).SetName("method");
        }
        public static Rule Entry()
        {
            return Store("entry", Seq(Token("entry"), CodeBlock())).SetName("entry");
        }
        public static Rule Transition()
        {
            return Store("transition", NoFailSeq(TypeExpr(), Token("->"), Name(), Eos())).SetName("transition");
        }
        public static Rule Transitions()
        {
            return Store("transitions", NoFailSeq(Token("transitions"), BracedGroup(Transition()))).SetName("transitions");
        }
        public static Rule State()
        {
            return Store("state", NoFailSeq(Name(), ArgList(), Token("{"), Opt(Entry()), Opt(Transitions()), Token("}"))).SetName("state");
        }
        public static Rule States()
        {
            return Store("states", NoFailSeq(Token("states"), BracedGroup(State()))).SetName("states");
        }
        public static Rule Annotation()
        {
            return Store("annotation", Seq(Ident(), Opt(Seq(CharSeq("="), SimpleExpr())))).SetName("annotation");
        }
        public static Rule Annotations()
        {
            return Store("annotations", NoFailSeq(Token("["), CommaList(Annotation()), Token("]"))).SetName("annotations");
        }
        public static Rule Implements()
        {
            return Store("implements", NoFailSeq(Token("implements"), BracedGroup(Seq(TypeExpr(), Eos())))).SetName("implements");
        }
        public static Rule Inherits()
        {
            return Store("inherits", NoFailSeq(Token("inherits"), BracedGroup(Seq(TypeExpr(), Eos())))).SetName("inherits");
        }
        public static Rule ClassBody()
        {
            return Braced(Seq(Opt(Inherits()), Opt(Implements()), Opt(Methods()), Opt(Fields()), Opt(States()))).SetName("class definition");
        }
        public static Rule Class()
        {
            return Store("class", Seq(Opt(Annotations()), NoFailSeq(Token("class"), Name(), ClassBody()))).SetName("class");
        }
        public static Rule Interface()
        {
            return Store("interface", Seq(Opt(Annotations()), NoFailSeq(Token("interface"), Name(),
                Braced(Seq(Opt(Inherits()), Opt(Methods())))))).SetName("interface");
        }
        public static Rule EnumValue()
        {
            return Seq(Name(), Eos()).SetName("enumerated value");
        }
        public static Rule EnumValues()
        {
            return Store("values", BracedGroup(EnumValue())).SetName("enumerated value group");
        }
        public static Rule Enum()
        {
            return Store("enum", Seq(Opt(Annotations()), NoFailSeq(Token("enum"), Name(),
                EnumValues()))).SetName("enumeration");
        }
        public static Rule ModuleElement()
        {
            return Choice(Class(), Interface(), Enum()).SetName("module element");
        }
        public static Rule Fields()
        {
            return Store("fields", NoFailSeq(Token("fields"), BracedGroup(Field()))).SetName("fields");
        }
        public static Rule Methods()
        {
            return Store("methods", NoFailSeq(Token("methods"), BracedGroup(Method()))).SetName("methods");
        }
       public static Rule Import()
        {
            return Store("import", NoFailSeq(Name(), Eos())).SetName("import");
        }
        public static Rule Imports()
        {
            return StoreBracedGroupIf("imports", Import());
        }
        public static Rule Module()
        {
            return StoreNoFailSeqIf("module", "module", new Rule[] { Name(), BracedGroup(ModuleElement()) }).SetName("module");
        }
        #endregion
    }
}
