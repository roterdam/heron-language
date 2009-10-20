/// Heron language interpreter for Windows in C#
/// http://www.heron-language.com
/// Copyright (c) 2009 Christopher Diggins
/// Licenced under the MIT License 1.0 
/// http://www.opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Reflection;
using System.Diagnostics;

namespace HeronEngine
{
    public static class Config
    {
        public static List<string> libraryPath = new List<string>();
        public static string testPath = "";
        public static bool runTestFiles = true;
        public static bool runUnitTests = true;
        public static bool tracing = false;
        public static List<string> libs = new List<string>();

        public static void LoadFromFile(string s)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(s);
            XmlElement root = doc.DocumentElement;
            XmlElement version = doc.FirstChild as XmlElement;
            // TODO: process version if need be
            foreach (XmlElement e in root.GetElementsByTagName("section"))
            {
                string sectionName = "";
                if (e.HasAttribute("name"))     
                    sectionName = e.GetAttribute("name");
                foreach (XmlNode n in e.ChildNodes)
                    ProcessElement(n as XmlElement, sectionName);
            }
        }

        static void ProcessElement(XmlElement e, string sectionName)
        {
            if (e == null)
                return;

            if (!e.HasAttribute("name"))
                throw new Exception("Missing name field in configuration file");

            switch (e.GetAttribute("name"))
            {
                case "library":
                    libraryPath = ProcessPathList(e);
                    break;
                case "testfiles":
                    testPath = ProcessPath(e);
                    break;
                case "runtestfiles":
                    runTestFiles = ProcessBool(e);
                    break;
                case "rununittests":
                    runUnitTests = ProcessBool(e);
                    break;
                case "inputlibs":
                    libs = ProcessStringList(e);
                    break;
            }
        }

        static string ProcessPath(XmlElement e)
        {
            string r = e.InnerXml;
            if (e.HasAttribute("relative") && e.GetAttribute("relative") == "true")
                r = Util.Util.GetExeDir() + "\\" + r;
            return r;
        }

        static List<string> ProcessPathList(XmlElement e)
        {
            List<string> paths = new List<string>();
            foreach (XmlElement path in e.GetElementsByTagName("path"))
                paths.Add(ProcessPath(path));
            return paths;
        }

        static bool ProcessBool(XmlElement e)
        {
            return e.InnerXml.Trim() == "true";
        }

        static string ProcessString(XmlElement e)
        {
            return e.InnerXml.Trim();
        }

        static List<string> ProcessStringList(XmlElement e)
        {
            List<string> r = new List<string>();
            foreach (XmlElement child in e.GetElementsByTagName("string"))
                r.Add(ProcessString(child));
            return r;
        }
    }
}
