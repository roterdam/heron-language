/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Peg
{
    /// <summary>
    /// A grammar is a set of rules which define a language. Grammar rules in the context 
    /// of a recursive descent parsing library correspond to pattern matches which are also known
    /// somewhat confusingly as "parsers". 
    /// </summary>
    public class Grammar
    {
        /// <summary>
        /// Used with RuleDelay to make cicrcular rule references
        /// </summary>
        /// <returns></returns>
        public delegate Rule RuleDelegate();

        /// <summary>
        /// A rule class corresponds a PEG grammar production rule. 
        /// A production rule describes how to generate valid syntactic
        /// phrases in a programming language. A production rule also
        /// corresponds to a pattern matcher in a recursive-descent parser. 
        /// 
        /// Each instance of a Rule class has a Match function which 
        /// has the responsibility to look at the current input 
        /// (which is managed by a Parser object) and return true or false, 
        /// depending on whether the current input corresponds
        /// to the rule. The Match function will increment the parsers internal
        /// pointer as it successfully matches characters, but will also 
        /// restore the pointer if it fails. 
        /// 
        /// Some rules have extra responsibilities above and beyond matchin (
        /// such as throwing exceptions, or creating an AST) which are described below.
        /// </summary>
        public abstract class Rule
        {
            string name;
            Rule parent;
            List<Rule> children = new List<Rule>();

            /// <summary>
            /// Returns true if the rule matches the sub-string starting at the current location 
            /// in the parser object. 
            /// </summary>
            /// <param name="p"></param>
            /// <returns></returns>
            public abstract bool Match(ParserState p);

            public Rule SetName(string s)
            {
                name = s;
                return this;
            }

            public string GetName()
            {
                return name;
            }

            public string GetParentName()
            {
                if (parent == null)
                    return null;
                else
                {
                    string r = parent.GetName();

                    // If our parent does not have a name
                    // We go to its parent, and so on.
                    if (r == null)
                        r = parent.GetParentName();

                    return r;
                }
            }

            public override string ToString()
            {
                return name;
            }

            public Rule Add(Rule r)
            {
                children.Add(r);
                r.parent = this;
                return r;
            }

            public void AddRange(IEnumerable<Rule> x)
            {
                foreach (Rule r in x)
                    Add(r);
            }

            public Rule FirstChild
            {
                get
                {
                    if (children.Count > 0)
                        return children[0];
                    else
                        return null;
                }
            }

            public List<Rule> Children
            {
                get
                {
                    return children;
                }
            }

            public static Rule operator +(Rule x, Rule y)
            {
                if (x is SeqRule)
                {
                    x.Add(y);
                    return x;
                }
                else
                {
                    return new SeqRule(new Rule[] { x, y });
                }
            }

            public static Rule operator |(Rule x, Rule y)
            {
                if (x is ChoiceRule)
                {
                    x.Add(y);
                    return x;
                }
                else
                {
                    return new ChoiceRule(new Rule[] { x, y });
                }
            }
        }

        /// <summary>
        /// This associates a rule with a node in the abstract syntax tree (AST). 
        /// Even though one could automatically associate each production rule with
        /// an AST node it is very cumbersome and inefficient to create and parse. 
        /// In otherwords the grammar tree is not expected to correspond directly to 
        /// the syntax tree since much of the grammar is noise (e.g. whitespace).
        /// </summary>
        public class AstNodeRule : Rule 
        {
            string sLabel;

            public AstNodeRule(string sLabel, Rule r)
            {
                Trace.Assert(r != null);
                this.sLabel = sLabel;
                Add(r);
            }

            public override bool Match(ParserState p)
            {
                p.CreateNode(sLabel);
                bool result = FirstChild.Match(p);
                if (result)
                {
                    p.CompleteNode();
                }
                else
                {
                    p.AbandonNode();
                }
                return result;
            }
        }

        /// <summary>
        /// This rule is neccessary allows you to make recursive references in the grammar.
        /// If you don't use this rule in a cyclical rule reference (e.g. A ::= B C, B ::== A D)
        /// then you will end up with an infinite loop during grammar generation.
        /// </summary>
        public class DelayRule : Rule
        {
            RuleDelegate mDeleg;
            
            public DelayRule(RuleDelegate deleg)
            {
                Trace.Assert(deleg != null);
                mDeleg = deleg;
            }

            public override bool Match(ParserState p)
            {
                return mDeleg().Match(p);
            }
        }

        /// <summary>
        /// This causes a rule to throw an exception with a particular error message if 
        /// it fails to match. You would use this rule in a grammar once you know clearly what 
        /// you are trying to parse, and failure is clearly an error. In other words you are saying 
        /// that back-tracking is of no use. 
        /// </summary>
        public class NoFailRule : Rule
        {
            public NoFailRule(Rule r)
            {
                Trace.Assert(r != null);
                Add(r);
            }

            public override bool Match(ParserState p)
            {
                int store = p.GetIndex();

                if (!FirstChild.Match(p))
                    throw new ParsingException(p.GetInput(), store, p.GetIndex(), FirstChild, GetMsg());
                
                return true;
            }

            public String GetMsg()
            {
                string r;
                string name = FirstChild.GetName();
                if (name != null)
                    r = "expected " + name;
                else
                    r = "parsing rule failed";
                string parentName = GetParentName();
                if (parentName != null)
                    r += " while parsing " + parentName;
                return r;
            }
        }

        /// <summary>
        /// This corresponds to a sequence operator in a PEG grammar. This tries 
        /// to match a series of rules in order, if one rules fails, then the entire 
        /// group fails and the parser index is returned to the original state.
        /// </summary>
        public class SeqRule : Rule
        {
            public SeqRule()
            {
            
            }

            public SeqRule(Rule[] xs)
            {
                AddRange(xs);
            }

            public override bool Match(ParserState p)
            {
                int iter = p.GetIndex();
                foreach (Rule r in Children)
                {
                    if (!r.Match(p))
                    {
                        p.SetPos(iter);
                        return false;
                    }
                }
                return true;
            }

            public override string ToString()
            {
                int n = 0;
                string result = "(";
                foreach (Rule r in Children)
                {
                    if (n++ != 0) result += " ";
                    result += r.ToString();
                }
                return result + ")";
            }
        }

        /// <summary>
        /// This rule corresponds to a choice operator in a PEG grammar. This rule 
        /// is successful if any of the matching rules are successful. The ordering of the 
        /// rules imply precedence. This means that the grammar will be unambiguous, and 
        /// differentiates the grammar as a PEG grammar from a context free grammar (CFG). 
        /// </summary>
        public class ChoiceRule : Rule
        {
            public ChoiceRule(Rule[] xs)
            {
                AddRange(xs);
            }

            public override bool Match(ParserState p)
            {
                foreach (Rule r in Children)
                {
                    if (r.Match(p))
                        return true;
                }
                return false;
            }

            public override string ToString()
            {
                string result = "(";
                int n = 0;
                foreach (Rule r in Children) 
                {
                    if (n++ > 0) result += " ";
                    result += r.ToString();
                }
                return result + ")";
            }
        }

        /// <summary>
        /// This but attempts to match a optional rule. It always succeeds 
        /// whether the underlying rule succeeds or not.
        /// </summary>
        public class OptRule : Rule
        {
            public OptRule(Rule r)
            {
                Add(r);
            }

            public override bool Match(ParserState p)
            {
                FirstChild.Match(p);
                return true;
            }

            public override string ToString()
            {
                return FirstChild.ToString() + "?";
            }
        }

        /// <summary>
        /// This attempts to match a rule 0 or more times. It will always succeed,
        /// and will match the rule as often as possible. Unlike the * operator 
        /// in PERL regular expressions, partial backtracking is not possible. 
        /// </summary>
        public class StarRule : Rule
        {
            public StarRule(Rule r)
            {
                Add(r);
            }

            public override bool Match(ParserState p)
            {
                while (FirstChild.Match(p))
                { }
                return true;
            }

            public override string ToString()
            {
                return FirstChild.ToString() + "*";
            }
        }

        /// <summary>
        /// This is similar to the StarRule except it matches a rule 1 or more times. 
        /// </summary>
        public class PlusRule : Rule
        {
            public PlusRule(Rule r)
            {
                Add(r);
            }

            public override bool Match(ParserState p)
            {
                if (!FirstChild.Match(p))
                    return false;
                while (FirstChild.Match(p))
                { }
                return true;
            }

            public override string ToString()
            {
                return FirstChild.ToString() + "+";
            }
        }

        /// <summary>
        /// Asssures that no more input exists
        /// </summary>
        public class EndOfInputRule : Rule
        {
            public override bool Match(ParserState p)
            {
                return p.AtEnd();
            }

            public override string ToString()
            {
                return "_eof_";
            }
        }

        /// <summary>
        /// This returns true if a rule can not be matched.
        /// It never advances the parser.
        /// </summary>
        public class NotRule : Rule
        {
            public NotRule(Rule r)
            {
                Add(r);
            }

            public override bool Match(ParserState p)
            {
                int pos = p.GetIndex();
                if (FirstChild.Match(p))
                {
                    p.SetPos(pos);
                    return false;
                }
                Trace.Assert(p.GetIndex() == pos);
                return true;
            }

            public override string ToString()
            {
                return "!" + FirstChild.ToString();
            }
        }

        /// <summary>
        /// This returns true if a rule can not be matched.
        /// It never advances the parser.
        /// </summary>
        public class AtRule : Rule
        {
            public AtRule(Rule r)
            {
                Add(r);
            }

            public override bool Match(ParserState p)
            {
                int pos = p.GetIndex();
                if (FirstChild.Match(p))
                {
                    p.SetPos(pos);
                    return true;
                }
                else
                {
                    p.SetPos(pos);
                    return false;
                }
            }

            public override string ToString()
            {
                return "=" + FirstChild.ToString();
            }
        }

        /// <summary>
        /// Attempts to match a specific character.
        /// </summary>
        public class SingleCharRule : Rule
        {
            public SingleCharRule(char x)
            {
                mData = x;
            }

            public override bool Match(ParserState p)
            {
                if (p.AtEnd()) 
                    return false;
                if (p.GetChar() == mData)
                {
                    p.GotoNext();
                    return true;
                }
                return false;
            }

            public override string ToString()
            {
                return mData.ToString();
            }

            char mData;
        }

        /// <summary>
        /// Attempts to match a sequence of characters.
        /// </summary>
        public class CharSeqRule : Rule
        {
            public CharSeqRule(string x)
            {
                mData = x;
            }

            public override bool Match(ParserState p)
            {
                if (p.AtEnd()) return false;
                int pos = p.GetIndex();
                foreach (char c in mData)
                {
                    if (p.GetChar() != c)
                    {
                        p.SetPos(pos);
                        return false;
                    }
                    p.GotoNext();
                }
                return true;
            }

            public override string ToString()
            {
                return mData;
            }

            string mData;
        }

        /// <summary>
        /// Matches any character and advances the parser, unless it is 
        /// at the end of the input.
        /// </summary>
        public class AnyCharRule : Rule
        {
            public override bool Match(ParserState p)
            {
                if (!p.AtEnd())
                {
                    p.GotoNext();
                    return true;
                }
                return false;
            }

            public override string ToString()
            {
                return ".";
            }
        }

        /// <summary>
        /// Returns true and advances the parser if the current character matches any 
        /// member of a specific set of characters.
        /// </summary>
        public class CharSetRule : Rule
        {
            public CharSetRule(string s)
            {
                mData = s;
            }

            public override bool Match(ParserState p)
            {
                if (p.AtEnd()) 
                    return false;
                foreach (char c in mData)
                {
                    if (c == p.GetChar())
                    {
                        p.GotoNext();
                        return true;
                    }
                }
                return false;
            }

            public override string ToString()
            {
                return "[" + mData + "]";
            }

            string mData;
        }

        /// <summary>
        /// Returns true and advances the parser if the current character matches any 
        /// member of a specific range of characters.
        /// </summary>
        public class CharRangeRule : Rule
        {
            public CharRangeRule(char first, char last)
            {
                mFirst = first;
                mLast = last;
                Trace.Assert(mFirst < mLast);
            }

            public override bool Match(ParserState p)
            {
                if (p.AtEnd()) return false;
                if (p.GetChar() >= mFirst && p.GetChar() <= mLast)
                {
                    p.GotoNext();
                    return true;
                }
                return false;
            }

            public override string ToString()
            {
                return "[" + mFirst.ToString() + ".." + mLast.ToString() + "]";
            }

            char mFirst;
            char mLast;
        }

        /// <summary>
        /// Matches a rule over and over until a terminating rule can be 
        /// successfully matched. 
        /// </summary>
        public class WhileNotRule : Rule
        {
            public WhileNotRule(Rule elem, Rule term)
            {
                mElem = elem;
                mTerm = term;
            }

            public override bool Match(ParserState p)
            {
                int pos = p.GetIndex();
                while (!mTerm.Match(p))
                {
                    if (!mElem.Match(p))
                    {
                        p.SetPos(pos);
                        return false;
                    }
                }
                return true;
            }

            public override string ToString()
            {
                return "((" + mElem.ToString() + " !" + mTerm.ToString() + ")* " + mTerm.ToString() + ")";
            }

            Rule mElem;
            Rule mTerm;
        }

        public static Rule EndOfInput() { return new EndOfInputRule(); }
        public static Rule Delay(RuleDelegate r) { return new DelayRule(r); }
        public static Rule SingleChar(char c) { return new SingleCharRule(c); }
        public static Rule CharSeq(string s) { return new CharSeqRule(s); }
        public static Rule AnyChar() { return new AnyCharRule(); }
        public static Rule NotChar(char c) { return Not(SingleChar(c)) + AnyChar(); }
        public static Rule CharSet(string s) { return new CharSetRule(s); }
        public static Rule CharRange(char first, char last) { return new CharRangeRule(first, last); }
        public static Rule Store(string name, Rule x) { return new AstNodeRule(name, x); }
        
        public static Rule NoFail(Rule x) 
        {
            if (x is SeqRule)
            {
                SeqRule r = new SeqRule();
                foreach (Rule child in x.Children)
                    r.Add(NoFail(child));
                return r;
            }
            else
            {
                return new NoFailRule(x);
            }
        }
        
        /*
        public static Rule NoFailSeq(Rule[] x) 
        { 
            Trace.Assert(x.Length >= 2);
            Rule[] ts = new Rule[x.Length];
            ts[0] = x[0];
            for (int i = 1; i < x.Length; ++i)
                ts[i] = NoFail(x[i]);
            return new SeqRule(ts);
        }

        public static Rule NoFailSeq(Rule x0, Rule x1) { return NoFailSeq(new Rule[] { x0, x1 }); }
        public static Rule NoFailSeq(Rule x0, Rule x1, Rule x2) { return NoFailSeq(new Rule[] { x0, x1, x2 }); }
        public static Rule NoFailSeq(Rule x0, Rule x1, Rule x2, Rule x3) { return NoFailSeq(new Rule[] { x0, x1, x2, x3 }); }
        public static Rule NoFailSeq(Rule x0, Rule x1, Rule x2, Rule x3, Rule x4) { return NoFailSeq(new Rule[] { x0, x1, x2, x3, x4 }); }
        public static Rule NoFailSeq(Rule x0, Rule x1, Rule x2, Rule x3, Rule x4, Rule x5) { return NoFailSeq(new Rule[] { x0, x1, x2, x3, x4, x5 }); }
        public static Rule NoFailSeq(Rule x0, Rule x1, Rule x2, Rule x3, Rule x4, Rule x5, Rule x6) { return NoFailSeq(new Rule[] { x0, x1, x2, x3, x4, x5, x6 }); }
        public static Rule NoFailSeq(Rule x0, Rule x1, Rule x2, Rule x3, Rule x4, Rule x5, Rule x6, Rule x7) { return NoFailSeq(new Rule[] { x0, x1, x2, x3, x4, x5, x6, x7 }); }
         */

        public static Rule Opt(Rule x) { return new OptRule(x); }
        public static Rule Star(Rule x) { return new StarRule(x); }
        public static Rule Plus(Rule x) { return new PlusRule(x); }
        public static Rule Not(Rule x) { return new NotRule(x); }
        public static Rule WhileNot(Rule elem, Rule term) { return new WhileNotRule(elem, term); }

        public static Rule NL() { return CharSet("\n").SetName("new line"); }
        public static Rule LowerCaseLetter() { return CharRange('a', 'z').SetName("lower case letter"); }
        public static Rule UpperCaseLetter() { return CharRange('A', 'Z').SetName("upper case letter"); }
        public static Rule Letter() { return (LowerCaseLetter() | UpperCaseLetter()).SetName("letter"); }
        public static Rule Digit() { return CharRange('0', '9').SetName("digit"); }
        public static Rule HexDigit() { return (Digit() | CharRange('a', 'f') | CharRange('A', 'F')).SetName("hexadecimal digit"); }
        public static Rule BinaryDigit() { return CharSet("01").SetName("binary digit"); }
        public static Rule IdentFirstChar() { return (SingleChar('_') | Letter()).SetName("identifier first character"); }
        public static Rule IdentNextChar() { return (IdentFirstChar() | Digit()).SetName("identifier next character"); }
        public static Rule Ident() { return (IdentFirstChar() + Star(IdentNextChar())).SetName("identifier"); }
        public static Rule EOW() { return Not(IdentNextChar()).SetName("end of word"); }
    }
}
