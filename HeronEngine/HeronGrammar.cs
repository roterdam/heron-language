/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;
using System.Collections.Generic;
using System.Text;
using Peg;

namespace HeronEngine
{
    public class HeronGrammar : Grammar
    {
        #region non-specific rules
        public static Rule UntilEndOfLine()
        {
            return NoFail(WhileNot(AnyChar(), NL()), "expected a new line");
        }
        public static Rule LineComment() 
        { 
            return Seq(CharSeq("//"), UntilEndOfLine()); 
        }
        public static Rule BlockComment()
        {
            return Seq(CharSeq("/*"), NoFail(WhileNot(AnyChar(), CharSeq("*/")), "expected a new line"));
        }
        public static Rule Comment() 
        { 
            return Choice(BlockComment(), LineComment()); 
        }
        public static Rule WS() 
        { 
            return Star(Choice(CharSet(" \t\n\r"), Comment())); 
        }
        public static Rule Symbol()
        {
            return Choice(CharSet(",."), Plus(CharSet("~`!@#$%^&*-+|:<>=?/")));
        }
        public static Rule Token(string s) 
        {
            return Token(CharSeq(s));
        }
        public static Rule Token(Rule r) 
        {
            return Seq(r, WS()); 
        }
        public static Rule Word(string s) 
        {
            return Seq(CharSeq(s), EOW(), WS()); 
        }
        public static Rule IntegerLiteral() 
        { 
            return Store("int", Seq(Opt(SingleChar('-')), Plus(Digit()))); 
        }
        public static Rule EscapeChar() 
        { 
            return Seq(SingleChar('\\'), AnyChar()); 
        }
        public static Rule StringCharLiteral() 
        { 
            return Choice(EscapeChar(), NotChar('"')); 
        }
        public static Rule CharLiteral() 
        {
            return Store("char", Seq(SingleChar('\''), StringCharLiteral(), SingleChar('\'')));
        }
        public static Rule StringLiteral() 
        { 
            return Store("string", Seq(SingleChar('\"'), Star(StringCharLiteral()), SingleChar('\"'))); 
        }
        public static Rule FloatLiteral() 
        {
            return Store("float", Seq(Opt(SingleChar('-')), Plus(Digit()), SingleChar('.'), Plus(Digit()))); 
        }
        public static Rule HexValue()
        {
            return Store("hex", Plus(HexDigit()));
        }
        public static Rule HexLiteral()
        {
            return Seq(CharSeq("0x"), NoFail(HexValue(), "expected at least one hexadecimal digit"));
        }
        public static Rule BinaryValue()
        {
            return Store("bin", Plus(BinaryDigit()));
        }
        public static Rule BinaryLiteral()
        {
            return Seq(CharSeq("0b"), NoFail(BinaryValue(), "expected at least one binary digit"));
        }
        public static Rule NumLiteral()
        {            
            return Choice(HexLiteral(), BinaryLiteral(), FloatLiteral(), IntegerLiteral());
        }
        public static Rule Literal() 
        {
            return Seq(Choice(StringLiteral(), CharLiteral(), NumLiteral()), WS()); 
        }        
        public static Rule Name() 
        {
            return Token(Store("name", Choice(Symbol(), Ident())));
        }
        public static Rule CommaList(Rule r)
        {
            return Opt(Seq(r, Star(Seq(Token(","), NoFail(r)))));
        }
        #endregion

        #region Heron-specific rules
        public static Rule TypeArgs() 
        {
		    return Store("typeargs", NoFailSeq(Token("<"), Delay(TypeExpr), Token(">")));
        }

        public static Rule NameOrLiteral()
        {
            // The order here is different, otherwise "-3" gets parsed as a name ('-')
            // followed by a literal ('3')
            return Choice(Literal(), Name());
        }

        /// <summary>
        /// A type name could be an ordinayr
        /// </summary>
        /// <returns></returns>
        public static Rule TypeName()
        {
            return Store("name", Seq(Ident(), Star(Seq(Token("."), NoFail(Ident(), "Expected identifier")))));
        }

	    public static Rule TypeExpr() 
        {
            return Store("type", Seq(TypeName(), Opt(TypeArgs()), WS()));
        }

	    public static Rule TypeDecl()
        {
            return Seq(Token(":"), NoFail(TypeExpr(), "expected type expression"));
        }
       
        public static Rule Arg() 
        {
            return Store("arg", Seq(Name(), Opt(TypeDecl())));
        }
        
        public static Rule ArgList() 
        {
		    return Store("arglist", Seq(Token("("), CommaList(Arg()), NoFail(Token(")"), "expected closing paranthesis")));
        }

        /* TEMP: removed because of problem with paring "arglist"
        public static Rule AnonFxn() 
        {
            return Store("anonfxn", Seq(ArgList(), Token("=>"), NoFail(CodeBlock())));
        }*/

        public static Rule DeleteStatement()
        {
            return Store("delete", NoFailSeq(Token("delete"), Expr()));
        }

        public static Rule Paranthesized(Rule r)
        {
            return NoFailSeq(Token("("), r, Token(")"));
        }

        public static Rule ParanthesizedExpr() 
        {
            return Store("paranexpr", Paranthesized(Opt(Delay(Expr))));
        }

        public static Rule Bracketed(Rule r)
        {   
            return NoFailSeq(Token("["), r, Token("]"));
        }

        public static Rule BracketedExpr()
        {
            return Store("bracketedexpr", Bracketed(Opt(Delay(Expr))));
        }

        public static Rule NewExpr()
        {
            return Store("new", NoFailSeq(Token("new"), TypeExpr(), ParanthesizedExpr()));
        }

        public static Rule SimpleExpr() 
        {
            return Choice(NewExpr(), Name(), Literal(), 
                // AnonFxn(), TEMP: removed 
                ParanthesizedExpr(), BracketedExpr());
		}

        public static Rule Expr() 
        {
            return Store("expr", Plus(SimpleExpr()));
        }
        
        public static Rule Initializer() 
        {
            return NoFailSeq(Token("="), Expr());
        }
        
        public static Rule VarDecl()
        {
            return Store("vardecl", NoFailSeq(Token("var"), Name(), Opt(TypeDecl()), Opt(Initializer()), Eos()));
        }

	    public static Rule ElseStatement()
        {
		    return NoFailSeq(Token("else"), Delay(Statement));
        }
        public static Rule IfStatement()
        {
            return Store("if", NoFailSeq(Token("if"), ParanthesizedExpr(), Delay(Statement), Opt(ElseStatement())));
        }
	    public static Rule ForEachStatement()
        {
		    return Store("foreach", NoFailSeq(Token("foreach"), Paranthesized(Seq(Name(),
                Token("in"), Expr())), Delay(Statement)));
        }
        public static Rule ForStatement()
        {
            return Store("for", NoFailSeq(Token("for"), Paranthesized(Seq(Name(),
                Initializer(), Eos(), Expr(), Eos(), Expr())), Delay(Statement)));
        }
        public static Rule Eos()
        {
            return Token(";");
        }
	    public static Rule ExprStatement()
        {
		    return Store("exprstatement", Seq(Expr(), Eos()));
        }
        public static Rule ReturnStatement()
        {
            return Store("return", NoFailSeq(Token("return"), Expr(), Eos()));
        }
	    public static Rule CaseStatement()
        {
            return Store("case", NoFailSeq(Token("case"), ParanthesizedExpr(), CodeBlock()));
        }
	    public static Rule DefaultStatement()
        {
            return Store("default", NoFailSeq(Token("default"), CodeBlock()));
        }
        public static Rule SwitchStatement()
        {
            return Store("switch", NoFailSeq(Token("switch"), ParanthesizedExpr(), Token("{"),
                Star(CaseStatement()), Opt(DefaultStatement()), Token("}")));
        }
	    public static Rule WhileStatement()
        {
            return Store("while", NoFailSeq(Token("while"), ParanthesizedExpr(), Delay(Statement)));
        }
	    public static Rule EmptyStatement()
        {
            return Eos();
        }
	    public static Rule Statement()
        {
            return Choice(
                Choice(CodeBlock(), VarDecl(), IfStatement(), SwitchStatement(), ForEachStatement(), ForStatement()),
Choice(WhileStatement(), ReturnStatement(), DeleteStatement(), ExprStatement(), EmptyStatement()));
        }
        public static Rule Braced(Rule r)
        {
            return Seq(Token("{"), NoFail(r, "expected '}'"), NoFail(Token("}")));
        }
        public static Rule BracedGroup(Rule r)
        {
            return Braced(Star(r));
        }
        public static Rule Field()
        {
            return Store("attribute", NoFailSeq(Name(), Opt(TypeDecl()), Eos()));
        }        
        public static Rule CodeBlock()
        {
            return Store("codeblock", Seq(Token("{"), Star(Delay(Statement)), NoFail(Token("}"), "expected '}' or statement")));
        }
        public static Rule FunDecl()
        {
            return Store("fundecl", Seq(Name(), NoFail(ArgList(), "expected argument list"), Opt(TypeDecl())));
        }
        public static Rule Method()
        {
            return Store("method", Seq(FunDecl(), NoFail(Choice(Eos(), CodeBlock()), "expected ';' or code block")));
        }
        public static Rule Entry()
        {
            return Store("entry", Seq(Token("entry"), CodeBlock()));
        }
        public static Rule Transition()
        {
            return Store("transition", NoFailSeq(TypeExpr(), Token("->"), Name(), Eos()));
        }
        public static Rule Transitions()
        {
            return Store("transitions", NoFailSeq(Token("transitions"), BracedGroup(Transition())));
        }
        public static Rule State()
        {
            return Store("state", NoFailSeq(Name(), ArgList(), Token("{"), Opt(Entry()), Opt(Transitions()), Token("}"))); 
        }
        public static Rule States()
        {
            return Store("states", NoFailSeq(Token("states"), BracedGroup(State())));
        }
        public static Rule Annotation()
        {
            return Store("annotation", Seq(Ident(), Opt(Seq(CharSeq("="), SimpleExpr()))));
        }
        public static Rule Annotations()
        {
            return Store("annotations", NoFailSeq(Token("["), CommaList(Annotation()), Token("]")));
        }
        public static Rule Implements()
        {
            return Store("implements", NoFailSeq(Token("implements"), BracedGroup(Seq(TypeExpr(), Eos()))));
        }
        public static Rule Inherits()
        {
            return Store("inherits", NoFailSeq(Token("inherits"), BracedGroup(Seq(TypeExpr(), Eos()))));
        }
        public static Rule Class()
        {
            return Store("class", Seq(Opt(Annotations()), NoFailSeq(Token("class"), Name(), 
                Braced(Seq(Opt(Inherits()), Opt(Implements()), Opt(Methods()), Opt(Fields()), Opt(States())))))); 
        }
        public static Rule Interface()
        {
            return Store("interface", Seq(Opt(Annotations()), NoFailSeq(Token("interface"), Name(), 
                Braced(Seq(Opt(Inherits()), Opt(Methods()))))));
        }
        public static Rule TopLevel()
        {
            return Choice(Class(), Interface());
        }
        public static Rule Fields()
        {
            return Store("fields", NoFailSeq(Token("fields"), BracedGroup(Field())));
        }
        public static Rule Methods()
        {
            return Store("methods", NoFailSeq(Token("methods"), BracedGroup(Method())));
        }
       public static Rule Import()
        {
            return Store("import", NoFailSeq(Name(), Eos()));
        }
        public static Rule Imports()
        {
            return Store("imports", NoFailSeq(Token("imports"), BracedGroup(Import())));
        }
        public static Rule Module()
        {
            return Store("module", NoFailSeq(Token("module"), Name(), BracedGroup(TopLevel())));
        }
        #endregion
    }
}
