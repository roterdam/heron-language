/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Peg
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
        List<Rule> children = new List<Rule>();

        /// <summary>
        /// Returns true if the rule matches the sub-string starting at the current location 
        /// in the parser object. 
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public abstract bool Match(ParserState p);

        public string Name
        {
            get
            {
                if (name == null)
                    return "???";
                else
                    return name;
            }
            set
            {
                if (value != null)
                    name = value;
            }
        }

        public override string ToString()
        {
            if (name == null)
                return Defn;
            else
                return name;
        }

        public string FullDefn
        {
            get
            {
                string defn = Defn;
                if (defn == null)
                    defn = "???";
                return Name + " ::== " + defn;
            }
        }

        public abstract string Defn { get; }

        public Rule Add(Rule r)
        {
            Trace.Assert(r != null);
            children.Add(r);
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

        public static Rule operator+(Rule x, Rule y)
        {
            return new SeqRule(new Rule[] { x, y });
        }

        public static Rule operator|(Rule x, Rule y)
        {
            return new ChoiceRule(new Rule[] { x, y });
        }
    }

    /// <summary>
    /// This associates a rule with a node in the abstract syntax tree (AST). 
    /// Even though one could automatically associate each production rule with
    /// an AST node it is very cumbersome and inefficient to create and parse. 
    /// In otherwords the grammar tree is not expected to correspond directly to 
    /// the syntax tree since much of the grammar is noise (e.r. whitespace).
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

        public override string Defn
        {
            get
            {
                if (FirstChild != null) 
                    throw new Exception("Missing child node");
                return FirstChild.ToString();
            }
        }
    }

    /// <summary>
    /// This rule is neccessary allows you to make recursive references in the grammar.
    /// If you don't use this rule in a cyclical rule reference (e.r. A ::= B C, B ::== A D)
    /// then you will end up with an infinite loop during grammar generation.
    /// </summary>
    public class RecursiveRule : Rule
    {
        RuleDelegate mDeleg;

        public RecursiveRule(RuleDelegate deleg)
        {
            Trace.Assert(deleg != null);
            mDeleg = deleg;
        }

        public override bool Match(ParserState p)
        {
            return mDeleg().Match(p);
        }

        public override string Defn
        {
            get
            {
                return "_recursive_";
            }
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
                throw new ParsingException(p.GetInput(), store, p.GetIndex(), FirstChild, Msg);

            return true;
        }

        public String Msg
        {
            get
            {
                string r;
                string name = FirstChild.Name;
                if (name == "???")
                    name = FirstChild.Defn;
                r = "expected " + name;
                return r;
            }
        }

        public override string Defn
        {
            get
            {
                if (FirstChild == null)
                    throw new Exception("Missing child node");
                return FirstChild.ToString() + "!";
            }
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

        public string BasicDefn
        {
            get
            {
                StringBuilder result = new StringBuilder();
                int n = 0;
                foreach (Rule r in Children)
                {
                    if (n++ != 0) result.Append(" + ");
                    if (r is SeqRule)
                    {
                        result.Append((r as SeqRule).BasicDefn);
                    }
                    else
                    {
                        result.Append(r.ToString());
                    }
                }
                return result.ToString();
            }
        }

        public override string Defn
        {
            get
            {
                return "(" + BasicDefn + ")";
            }
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

        public string BasicDefn
        {
            get
            {
                StringBuilder result = new StringBuilder();
                int n = 0;
                foreach (Rule r in Children)
                {
                    if (n++ != 0) result.Append(" | ");
                    if (r is ChoiceRule)
                    {
                        result.Append((r as ChoiceRule).BasicDefn);
                    }
                    else
                    {
                        result.Append(r.ToString());
                    }
                }
                return result.ToString();
            }
        }

        public override string Defn
        {
            get
            {
                return "(" + BasicDefn + ")";
            }
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
            Trace.Assert(r != null);
            Add(r);
        }

        public override bool Match(ParserState p)
        {
            FirstChild.Match(p);
            return true;
        }

        public override string Defn
        {
            get
            {
                if (FirstChild != null) 
                    throw new Exception("Missing child node");
                return FirstChild.ToString() + "?";
            }
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
            Trace.Assert(r != null);
            Add(r);
        }

        public override bool Match(ParserState p)
        {
            while (FirstChild.Match(p))
            { }
            return true;
        }

        public override string Defn
        {
            get
            {
                if (FirstChild == null)
                    throw new Exception("Missing child node");
                return FirstChild.ToString() + "*";
            }
        }
    }

    /// <summary>
    /// This is similar to the StarRule except it matches a rule 1 or more times. 
    /// </summary>
    public class PlusRule : Rule
    {
        public PlusRule(Rule r)
        {
            Trace.Assert(r != null);
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

        public override string Defn
        {
            get
            {
                if (FirstChild == null)
                    throw new Exception("Missing child node");
                return FirstChild.ToString() + "+";
            }
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

        public override string Defn
        {
            get
            {
                return "_eof_";
            }
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
            Trace.Assert(r != null);
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

        public override string Defn
        {
            get
            {
                if (FirstChild == null)
                    throw new Exception("Missing child name");
                return FirstChild.ToString() + "^";
            }
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
            Trace.Assert(r != null);
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

        public override string Defn
        {
            get
            {
                if (FirstChild == null)
                    throw new Exception("Missing child node");
                return "=" + FirstChild.ToString();
            }
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

        public override string Defn
        {
            get
            {
                return "[" + mData.ToString() + "]";
            }
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
            int pos = p.GetIndex();
            foreach (char c in mData)
            {
                if (p.AtEnd())
                    return false;

                if (p.GetChar() != c)
                {
                    p.SetPos(pos);
                    return false;
                }
                p.GotoNext();
            }
            return true;
        }

        public override string Defn
        {
            get
            {
                return "<" + mData + ">";
            }
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

        public override string Defn
        {
            get
            {
                return ".";
            }
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

        public override string Defn
        {
            get
            {
                if (mData == null)
                    throw new Exception("Missing child node");
                return "[" + mData + "]";
            }
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

        public override string Defn
        {
            get
            {
                return "[" + mFirst.ToString() + ".." + mLast.ToString() + "]";
            }
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
            Trace.Assert(elem != null);
            Trace.Assert(term != null);
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

        public override string Defn
        {
            get
            {
                if (mElem == null)
                    throw new Exception("Missing 'element' node");
                if (mTerm == null)
                    throw new Exception("Missing 'termination' node");
                return "((" + mElem.ToString() + " !" + mTerm.ToString() + ")* " + mTerm.ToString() + ")";
            }
        }

        Rule mElem;
        Rule mTerm;
    }
}