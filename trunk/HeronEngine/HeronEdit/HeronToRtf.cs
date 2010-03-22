using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using HeronEngine;

namespace HeronEdit
{
    class HeronToRtf
    {
        /// <summary>
        /// list of colors used
        /// </summary>
        private List<Color> colors = new List<Color>();

        enum TokenType
        {
            Unknown,
            Operator,
            WSpace,
            Identifier,
            Char,
            String,
            VerbString,
            Number,
            Keyword,
            LineComment,
            FullComment,
        }

        public HeronToRtf()
        {
            colors.Add(Color.Blue);
            colors.Add(Color.Navy);
            colors.Add(Color.Red);
            colors.Add(Color.Khaki);
            colors.Add(Color.Orange);
            colors.Add(Color.Purple);
            colors.Add(Color.Lavender);
        }

        private static string ColorToRtfTableEntry(Color c)
        {
            return 
                @"\red" + c.R.ToString() + 
                @"\blue" + c.B.ToString() + 
                @"\green" + c.G.ToString(); 
        }

        /// <summary>
        /// Returns 1 + the index of the associated color index in the 
        /// RTF color table. 
        /// </summary>
        /// <param name="tt"></param>
        /// <returns></returns>
        private int TokenTypeToColorIndex(TokenType tt)
        {
            switch (tt)
            {
                case TokenType.Identifier:
                    return 1;
                case TokenType.Keyword:
                    return 2;
                case TokenType.String:
                    return 3;
                case TokenType.Number:
                    return 4;
                case TokenType.Char:
                    return 5;
                case TokenType.VerbString:
                    return 6;
                case TokenType.LineComment:
                    return 7;
                case TokenType.FullComment:
                    return 7;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Creates a RTF color table.
        /// </summary>
        /// <returns>the color table as a string</returns>
        private string GetColorTable()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"{\colortbl ;");
            foreach (Color c in colors)
            {
                sb.Append(ColorToRtfTableEntry(c));
                sb.Append(';');
            }
            sb.Append('}');

            return sb.ToString();
        }

        /// <summary>
        /// Given some rtf, it replaces the color table with a new one which has 
        /// the colors needed to color Heron syntax.
        /// </summary>
        /// <param name="rtf"></param>
        /// <returns></returns>
        private string InsertColorTable(String rtf)
        {
            // Remove old color table
            Regex reColorTbl = new Regex(@"{\colortbl[^}]*}");
            rtf = reColorTbl.Replace(rtf, "");

            // find index of start of header
            int n = rtf.IndexOf(@"\rtf");

            // Find location of first property ('{') or end of ('}')
            int a = rtf.IndexOf('{', n);
            int b = rtf.IndexOf('}', n);
            if (a == -1 || a > b) a = b;

            // Insert the color table.
            return rtf.Insert(a, GetColorTable());
        }

        /// <summary>
        /// Removes all color tags from rtf text
        /// </summary>
        /// <param name="rtf"></param>
        /// <returns></returns>
        private string StripColor(String rtf)
        {
            Regex colorCodes1 = new Regex(@"\\cf. ", RegexOptions.Compiled);
            rtf = colorCodes1.Replace(rtf, "");
            Regex colorCodes2 = new Regex(@"\\cf.\\", RegexOptions.Compiled);
            rtf = colorCodes2.Replace(rtf, @"\");
            return rtf;
        }

        /// <summary>
        /// Returns a RTF string segement with coloring 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        private string AddColor(string s, int n)
        {
            if (n == 0)
                return s;
            StringBuilder sb = new StringBuilder();
            sb.Append(@"\cf");
            sb.Append(n);
            sb.Append(' ');
            sb.Append(s);
            sb.Append(@"\cf0 ");
            return sb.ToString(); 
        }

        /// <summary>
        /// Adds coloring to a string based on the token type
        /// </summary>
        /// <param name="s"></param>
        /// <param name="tt"></param>
        /// <returns></returns>
        private string AddColor(string s, TokenType tt)
        {
            return AddColor(s, TokenTypeToColorIndex(tt));
        }

        public string ToRtf(string s)
        {
            StringBuilder sb = new StringBuilder();
            
            sb.Append(@"{\rtf1\ansi\ansicpg1252\deff0\deflang1033");
            sb.Append(@"{\fonttbl{\f0\fnil\fcharset0 Courier New;}}");
            sb.Append(GetColorTable());
            sb.Append("}");

            int i = 0;
            while (i < s.Length)
            {
                string token = "";

                if (ParseIdentifier(s, i, ref token))
                {
                    sb.Append(AddColor(token, TokenType.Identifier));
                    sb.Append(token);
                    i += token.Length;
                }
                else if (ParseWSpace(s, i, ref token))
                {
                    sb.Append(token);
                    sb.Append(token);
                    i += token.Length;
                }
                else if (ParseNumber(s, i, ref token))
                {
                    sb.Append(AddColor(token, TokenType.Number));
                    i += token.Length;
                }
                else if (ParseLineComment(s, i, ref token))
                {
                    sb.Append(AddColor(token, TokenType.LineComment));
                    i += token.Length;
                }
                else if (ParseFullComment(s, i, ref token))
                {
                    sb.Append(AddColor(token, TokenType.FullComment));
                    i += token.Length;
                }
                else if (ParseString(s, i, ref token))
                {
                    sb.Append(AddColor(token, TokenType.String));
                    i += token.Length;
                }
                else if (ParseVerbString(s, i, ref token))
                {
                    sb.Append(AddColor(token, TokenType.VerbString));
                    i += token.Length;
                }
                else if (ParseChar(s, i, ref token))
                {
                    sb.Append(AddColor(token, TokenType.Char));
                    i += token.Length;
                }
                else
                {
                    sb.Append(s[i++]);
                }
            }

            return sb.ToString();
        }

        private bool ParseChar(string s, int i, ref string token)
        {
            if (i >= s.Length - 3) return false;
            if (s[i] != '\'') return false;
            return Parser.Parse(HeronGrammar.CharLiteral, s, i, out token);
        }

        private bool ParseVerbString(string s, int i, ref string token)
        {
            if (i >= s.Length - 3) return false;
            if (s[i] != '@') return false;
            if (s[i + 1] != '"') return false;
            return Parser.Parse(HeronGrammar.VerbStringLiteral, s, i, out token);
        }

        private bool ParseString(string s, int i, ref string token)
        {
            if (i >= s.Length - 2) return false;
            if (s[i] != '"') return false;
            return Parser.Parse(HeronGrammar.StringLiteral, s, i, out token);
        }

        private bool ParseFullComment(string s, int i, ref string token)
        {
            if (i >= s.Length - 4) return false;
            if (s[i] != '/') return false;
            if (s[i + 1] != '*') return false;
            return Parser.Parse(HeronGrammar.BlockComment, s, i, out token);
        }

        private bool ParseLineComment(string s, int i, ref string token)
        {
            if (i >= s.Length - 3) return false;
            if (s[i] != '/') return false;
            if (s[i + 1] != '/') return false;
            return Parser.Parse(HeronGrammar.LineComment, s, i, out token);
        }

        private bool ParseNumber(string s, int i, ref string token)
        {
            if (i >= s.Length - 1) return false;
            if (!Char.IsDigit(s[i])) return false;
            return Parser.Parse(HeronGrammar.NumLiteral, s, i, out token);
        }

        private bool ParseWSpace(string s, int i, ref string token)
        {
            int begin = i;
            while (i < s.Length && Char.IsWhiteSpace(s[i]))
                ++i;
            if (i == begin)
                return false;
            token = s.Substring(begin, i - begin);
            return true;
        }

        private bool ParseIdentifier(string s, int i, ref string token)
        {
            if (i >= s.Length - 1) return false;
            return Parser.Parse(HeronGrammar.Ident, s, i, out token);
        }
    }
}
