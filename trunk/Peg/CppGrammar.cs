using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Peg;

namespace Peg
{
    public class CppGrammar : Grammar
    {
        #region C++ tokenizing rules
        public static Rule LineExt = CharSeq("\\") + EndOfLine;
        public static Rule AnyCharExceptEndOfLine = LineExt | AnyCharExcept(EndOfLine);
        public static Rule UntilEndOfLine = Star(AnyCharExceptEndOfLine);
        public static Rule UntilPastEndOfLine = UntilEndOfLine + EndOfLine;
        public static Rule LineComment = CharSeq("//") + Store("linecomment", UntilEndOfLine);
        public static Rule BlockComment = CharSeq("/*") + Store("blockcomment", AllCharsExcept(CharSeq("*/"))) + CharSeq("*/");
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

        #region tokenizing rules
        public static Rule Comma = CharSeq(",");
        public static Rule Symbol = Plus(CharSet(".~`!#$%^&*-+|:<>=?/"));
        public static Rule IntegerLiteral = Store("int", Opt(SingleChar('-')) + Plus(Digit));
        public static Rule EscapeChar = SingleChar('\\') + AnyChar;
        public static Rule StringCharLiteral = EscapeChar | (Not(CharSet("\n\"")) + AnyChar);
        public static Rule CharLiteral = Store("char", SingleChar('\'') + StringCharLiteral + SingleChar('\''));
        public static Rule StringLiteral = Store("string", (SingleChar('\"') + Star(StringCharLiteral) + SingleChar('\"')));
        public static Rule FloatLiteral = Store("float", Opt(SingleChar('-')) + Plus(Digit) + SingleChar('.') + Plus(Digit) + Opt(CharSeq("f")));
        public static Rule HexValue = Store("hex", Plus(HexDigit));
        public static Rule HexLiteral = CharSeq("0x") + NoFail(HexValue);
        public static Rule BinaryValue = Store("bin", Plus(BinaryDigit));
        public static Rule BinaryLiteral = CharSeq("0b") + NoFail(BinaryValue);
        public static Rule NumLiteral = HexLiteral | BinaryLiteral | FloatLiteral | IntegerLiteral;
        public static Rule Literal = Store("literal", StringLiteral | CharLiteral | NumLiteral) + WS;
        public static Rule Name = Store("name", Ident + WS);
        public static Rule NameOrLiteral = Name | Literal;
        public static Rule Eos = Token(";");
        #endregion

        #region pre-processor rules
        public static Rule PPChunks = Star(Delay("delayed chunk", () => PPChunk));
        public static Rule PPName = Store("PPname", Ident) + SingleLineWS;
        public static Rule PPLiteral = Store("PPliteral", StringLiteral | CharLiteral | NumLiteral) + SingleLineWS;
        public static Rule PPExpression = Store("PPexpr", PPName | PPLiteral);
        public static Rule PPToken(String s) { return CharSeq(s) + Not(IdentNextChar) + SingleLineWS; }  
        public static Rule PPParams = Store("PPparams", Token("(") + SingleLineWS + CommaList(PPName) + CharSeq(")") + SingleLineWS);
        public static Rule PPQuotedInclude = StoredDelimitedCharSeq("PPquotedinclude", SingleChar('"'), SingleChar('"')); 
        public static Rule PPAngledInclude = StoredDelimitedCharSeq("PPangledinclude", SingleChar('<'), SingleChar('>'));
        public static Rule PPInclude = Store("PPinclude", PPToken("#include") + NoFail((PPQuotedInclude | PPAngledInclude) + UntilPastEndOfLine));
        public static Rule PPDefineFunction = Store("PPfunction", PPToken("#define") + PPName + PPParams + UntilPastEndOfLine);
        public static Rule PPDefineNoFunction = Store("PPdefine", PPToken("#define") + PPName + Opt(PPExpression) + UntilPastEndOfLine);
        public static Rule PPUndefine = Store("PPundefine", PPToken("#undef") + PPName + UntilPastEndOfLine);
        public static Rule PPDefine = PPDefineFunction | PPDefineNoFunction;  // This order is very important 
        public static Rule PPElseIf = Store("PPelif", PPToken("#elif") + NoFail(PPExpression) + UntilPastEndOfLine + PPChunks);
        public static Rule PPElse = Store("PPelse", PPToken("#else") + UntilPastEndOfLine + PPChunks);
        public static Rule PPEndIf = Store("PPendif", PPToken("#endif") + UntilPastEndOfLine);
        public static Rule PPIf = Store("PPif", PPToken("#if") + Store("PPcondition", UntilEndOfLine) + EndOfLine + PPChunks + Star(PPElseIf) + Opt(PPElse) + NoFail(PPEndIf));
        public static Rule PPIfDef = Store("PPifdef", PPToken("#ifdef") + NoFail(PPExpression) + UntilPastEndOfLine + PPChunks + Star(PPElseIf) + Opt(PPElse) + NoFail(PPEndIf));
        public static Rule PPIfNDef = Store("PPifndef", PPToken("#ifndef") + NoFail(PPExpression) + UntilPastEndOfLine + PPChunks + Star(PPElseIf) + Opt(PPElse) + NoFail(PPEndIf));
        public static Rule PPPragma = Store("PPpragma", PPToken("#pragma") + UntilPastEndOfLine);
        public static Rule PPEmpty = Store("PPempty", SingleLineWS + Not(SingleChar('#')) + Star(Comment | (Not(EndOfLine) + AnyChar)) + EndOfLine);
        public static Rule PPDirective = Store("PPdirective", PPDefine | PPUndefine | PPInclude | PPIf | PPIfDef | PPIfNDef | PPPragma);
        public static Rule PPChunk = PPDirective | PPEmpty;
        public static Rule PPFile = PPChunks + NoFail(EndOfInput);
        #endregion 

        #region high-level rules
        public static Rule DelayedContent = Delay("anycontent", () => Content);
        public static Rule Atom = Store("atom", Literal | Symbol | Ident | Eos | Comma) + WS;
        public static Rule BracedContent = Store("braced", Token("{") + Star(DelayedContent) + NoFail(Token("}")));
        public static Rule ParanthesizedContent = Store("paranthesized", Token("(") + Star(DelayedContent) + NoFail(Token(")")));
        public static Rule BracketedContent = Store("bracketed", Token("[") + Star(DelayedContent) + NoFail(Token("]")));
        public static Rule Class = Store("class", Word("class") + NoFail(Name + (Eos | BracedContent)));
        public static Rule Struct = Store("struct", Word("struct") + NoFail(Name + (Eos | BracedContent)));
        public static Rule Union = Store("union", Word("union") + NoFail(Name + (Eos | BracedContent)));
        public static Rule Enumeration = Store("enum", Word("enum") + NoFail(Name + (Eos | BracedContent)));
        public static Rule Content = BracedContent | ParanthesizedContent | BracketedContent | Class | Struct | Union | Enumeration | Atom;
        public static Rule File = Store("ws", WS) + Star(Content) + WS + NoFail(EndOfInput);
        #endregion

        static CppGrammar()
        {
            AssignRuleNames(typeof(CppGrammar));
        }
    }
}
