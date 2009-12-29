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
    /// <summary>
    /// Singleton for managing configuration information for the whole engine.
    /// Uses default settings, but can be loaded and saved to an XML file. 
    /// </summary>
    public static class Config
    {
        public static List<string> inputPath = new List<string>();
        public static List<string> libs = new List<string>();
        public static List<string> extensions = new List<string>();
        public static bool runUnitTests = false;
        public static bool outputGrammar = false;
        public static bool outputPrimitives = false;

        public static void LoadFromFile(string s)
        {
            inputPath.Add(Util.GetExeDir());
            inputPath.Add(Util.GetExeDir() + @"/lib");
            extensions.Add(".heron");
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
                case "inputpath":
                    inputPath = ProcessPathList(e);
                    break;
                case "libs":
                    libs = ProcessStringList(e);
                    break;
                case "rununittests":
                    runUnitTests = ProcessBool(e);
                    break;
                case "outputgrammar":
                    outputGrammar = true;
                    break;
                case "outputprimitives":
                    outputPrimitives = true;
                    break;
                case "extensions":
                    extensions = ProcessStringList(e);
                    break;
            }
        }

        static string ProcessPath(XmlElement e)
        {
            string r = e.InnerXml;
            if (e.HasAttribute("relative") && e.GetAttribute("relative") == "true")
                r = Util.GetExeDir() + "\\" + r;
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
