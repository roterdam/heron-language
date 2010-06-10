/// Heron language interpreter for Windows in C#
/// http://www.heron-language.com
/// Copyright (c) 2009 Christopher Diggins
/// Licenced under the MIT License 1.0 
/// http://www.opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Diagnostics;

namespace HeronEngine
{
    public static class Util
    {
        public static Regex reWSpace = new Regex(@"\s+", RegexOptions.Singleline);

        public static string RemoveInternalWSpace(this string self)
        {
            return reWSpace.Replace(self, "");
        }

        public static string CompressWSpace(this string self)
        {
            return reWSpace.Replace(self.Trim(), " ");
        }

        public static bool IsValidIdentifier(this string self)
        {
            Regex re = new Regex(@"\w(\w|\d)*", RegexOptions.Compiled);
            Match m = re.Match(self);
            return m.Success && m.Length == self.Length;
        }

        public static string IndentLines(string s, string indent)
        {
            return s.Replace("\n", "\n" + indent);
        }

        public static string GetExeDir()
        {
            Assembly a = Assembly.GetExecutingAssembly();
            string s = a.Location;
            s = Path.GetDirectoryName(s);
            return s;
        }

        public static void SaveToFile(string sText, string sFile)
        {
            using (StreamWriter w = File.CreateText(sFile))
            {
                w.Write(sText);
                w.Flush();
                w.Close();
            }
        }

        public static string ReadFromFile(string sFile)
        {
            using (StreamReader r = new StreamReader(sFile))
            {
                string result = r.ReadToEnd();
                r.Close();
                return result;
            }
        }

        #region list extensions
        public static T Peek<T>(this List<T> self)
        {
            return self[self.Count - 1];
        }
        public static void Pop<T>(this List<T> self)
        {
            self.RemoveAt(self.Count - 1);
        }
        public static T Pull<T>(this List<T> self)
        {
            T r = self.Peek();
            self.Pop();
            return r;
        }
        public static bool IsEmpty<T>(this List<T> self)
        {
            return self.Count == 0;
        }
        #endregion

        public static int ParitionSize(int size, int n, int m)
        {
            if (n < 0 || n > m || size < 0 || m < 0)
                throw new ArgumentOutOfRangeException();
            int r = size / m;
            if (size % m > 0)
                r += 1;
            int begin = r * n;
            Debug.Assert(begin >= 0 && begin < size);
            if (r * n + r > size)
                r = size - begin;
            return r;
        }
    }
}
