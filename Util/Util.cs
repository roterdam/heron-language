using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace Util
{
    public static class Util
    {
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
    }
}
