/// Heron language interpreter for Windows in C#
/// http://www.heron-language.com
/// Copyright (c) 2009 Christopher Diggins
/// Licenced under the MIT License 1.0 
/// http://www.opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;

namespace HeronEngine
{
    /// <summary>
    /// A grammar is a set of rules which define a language. Grammar rules in the context 
    /// of a recursive descent parsing library correspond to pattern matches which are also known
    /// somewhat confusingly as "parsers". 
    /// </summary>
    public class Grammar
    {
        #region rule creating functions
        public static Rule Delay(string name, RuleDelegate r) { return new RecursiveRule(name, r); }
        public static Rule SingleChar(char c) { return new SingleCharRule(c); }
        public static Rule CharSeq(string s) { return new CharSeqRule(s); }
        public static Rule NotChar(char c) { return Not(SingleChar(c)) + AnyChar; }
        public static Rule CharSet(string s) { return new CharSetRule(s); }
        public static Rule CharRange(char first, char last) { return new CharRangeRule(first, last); }
        public static Rule Store(string name, Rule x) { return new ParseNodeRule(name, x); }
        public static Rule Opt(Rule x) { return new OptRule(x); }
        public static Rule Star(Rule x) { return new StarRule(x); }
        public static Rule Plus(Rule x) { return new PlusRule(x); }
        public static Rule Not(Rule x) { return new NotRule(x); }
        public static Rule WhileNot(Rule elem, Rule term) { return new WhileNotRule(elem, term); }
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
        #endregion

        #region static rule constants
        public static Rule AnyChar = new AnyCharRule();
        public static Rule EndOfInput = new EndOfInputRule();
        public static Rule NL = CharSet("\n");
        public static Rule LowerCaseLetter = CharRange('a', 'z');
        public static Rule UpperCaseLetter = CharRange('A', 'Z');
        public static Rule Letter = LowerCaseLetter | UpperCaseLetter;
        public static Rule Digit = CharRange('0', '9');
        public static Rule HexDigit = Digit | CharRange('a', 'f') | CharRange('A', 'F');
        public static Rule BinaryDigit = CharSet("01");
        public static Rule IdentFirstChar = SingleChar('_') | Letter;
        public static Rule IdentNextChar = IdentFirstChar | Digit;
        public static Rule Ident = IdentFirstChar + Star(IdentNextChar);
        public static Rule EOW = Not(IdentNextChar);
        #endregion

        #region miscellaneous grammar functions
        /// <summary>
        /// This function loops through all static rule fields in a class
        /// and assigns a name to them, which is the same as the field name.
        /// </summary>
        /// <param name="grammarType"></param>
        public static void AssignRuleNames(Type grammarType)
        {
            foreach (FieldInfo fi in grammarType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy ))
            {
                if (fi.FieldType.Equals(typeof(Rule)))
                {
                    Rule r = fi.GetValue(null) as Rule;
                    r.Name = fi.Name;
                }
            }
        }

        /// <summary>
        /// Converts the entire grammar into a text-file representation
        /// </summary>
        /// <param name="grammarType"></param>
        /// <returns></returns>
        public static string ToString(Type grammarType)
        {
            SortedList<string, FieldInfo> fields = new SortedList<string,FieldInfo>();
            foreach (FieldInfo fi in grammarType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy))
            {
               fields.Add(fi.Name, fi);
            }

            StringBuilder sb = new StringBuilder();
            foreach (FieldInfo fi in fields.Values)
            {
                if (typeof(Rule).IsAssignableFrom(fi.FieldType))
                {
                    Rule r = fi.GetValue(null) as Rule;
                    sb.AppendLine(r.FullDefn);
                }
            }
            return sb.ToString();
        }
        #endregion 
    }
}
