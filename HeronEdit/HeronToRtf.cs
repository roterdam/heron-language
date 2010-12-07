using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using HeronEngine;
using Peg;

namespace HeronEdit
{
    /// <summary>
    /// Converts a block of Heron code to rich text with syntax coloring.
    /// This contains the coloring scheme hard-coded. 
    /// </summary>
    class HeronToRtf
    {
        /// <summary>
        /// list of colors used
        /// </summary>
        private List<Color> colors = new List<Color>();

        /// <summary>
        /// Identifies the different kinds of tokens.
        /// </summary>
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
            BlockComment,
        }

        public HeronToRtf()
        {
            colors.Add(Color.Blue);
            colors.Add(Color.Navy);
            colors.Add(Color.Red);
            colors.Add(Color.DarkOliveGreen);
            colors.Add(Color.Orange);
            colors.Add(Color.Purple);
            colors.Add(Color.DarkCyan);
        }

        /// <summary>
        /// Converts a color to an entry in the RTF entry. 
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
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
                    return 2;
                case TokenType.Keyword:
                    return 1;
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
                case TokenType.BlockComment:
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

        /// <summary>
        /// Returns true if the string is a Heron keyword.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public bool IsKeyword(string s)
        {
            switch (s)
            {
                case "module":
                case "class":
                case "interface":
                case "enum":

                case "imports":
                case "inherits":
                case "fields":
                case "methods":
                case "implements":

                case "for":
                case "foreach":
                case "while":
                case "if":
                case "else":
                case "switch":
                case "case":
                case "default":

                case "var":
                case "return":
                case "new":
                case "delete":

                case "function":
                case "table":
                case "record":

                case "map":
                case "select":
                case "reduce":
                case "accumulate":

                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns a simple RTF header for the editor with Courier New 10pt 
        /// as the font size.
        /// </summary>
        /// <returns></returns>
        public string RtfHeader()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"{\rtf1\ansi\ansicpg1252\deff0\deflang1033");
            sb.Append(@"{\fonttbl{\f0\fnil\fcharset0 Courier New;}}");
            sb.AppendLine();
            sb.Append(GetColorTable());
            sb.AppendLine();
            sb.AppendLine(@"\viewkind4\uc1\pard\f0\fs17");
            return sb.ToString();
        }

        /// <summary>
        /// Converts a text string containing Heron into RTF 
        /// with a header. This could be assigned to the RTF property 
        /// of a rich text control.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public string ToRtf(string s)
        {
            StringBuilder sb = new StringBuilder();
            int i = 0;
            s = s.Replace(@"\", @"\\");
            s = s.Replace("{", "\\{");
            s = s.Replace("}", "\\}");

            while (i < s.Length)
            {
                string token = "";

                if (Parser.Parse(HeronGrammar.Ident, s, i, out token))
                {
                    if (IsKeyword(token))
                        sb.Append(AddColor(token, TokenType.Keyword));
                    else
                        sb.Append(AddColor(token, TokenType.Identifier));
                    i += token.Length;
                }
                else if (ParseWSpace(s, i, out token))
                {
                    sb.Append(token);
                    i += token.Length;
                }
                else if (Parser.Parse(HeronGrammar.NumLiteral, s, i, out token))
                {
                    sb.Append(AddColor(token, TokenType.Number));
                    i += token.Length;
                }
                else if (Parser.Parse(HeronGrammar.LineComment, s, i, out token))
                {
                    sb.Append(AddColor(token, TokenType.LineComment));
                    i += token.Length;
                }
                else if (Parser.Parse(HeronGrammar.BlockComment, s, i, out token))
                {
                    sb.Append(AddColor(token, TokenType.BlockComment));
                    i += token.Length;
                }
                else if (Parser.Parse(HeronGrammar.StringLiteral, s, i, out token))
                {
                    sb.Append(AddColor(token, TokenType.String));
                    i += token.Length;
                }
                else if (Parser.Parse(HeronGrammar.VerbStringLiteral, s, i, out token))
                {
                    sb.Append(AddColor(token, TokenType.VerbString));
                    i += token.Length;
                }
                else if (Parser.Parse(HeronGrammar.CharLiteral, s, i, out token))
                {
                    sb.Append(AddColor(token, TokenType.Char));
                    i += token.Length;
                }
                else
                {
                    sb.Append(s[i++]);
                }
            }
            s = sb.ToString();
            s = s.Replace("\n", "\\par\r\n");
            return RtfHeader() + s;
        }

        private bool ParseWSpace(string s, int i, out string token)
        {
            int begin = i;
            while (i < s.Length && Char.IsWhiteSpace(s[i]))
                ++i;
            if (i == begin)
            {
                token = "";
                return false;
            }
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
