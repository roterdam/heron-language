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
        public static int maxListPrintableSize = 5;
        public static int maxThreads = 1;
        public static bool showTiming = false;
        public static bool waitForKeypress = true;
        public static bool optimize = false;

        static Config()
        {
            inputPath.Add(Util.GetExeDir());
            inputPath.Add(Util.GetExeDir() + "\\lib");
            extensions.Add(".heron");
        }

        public static void LoadFromFile(string s)
        {
            XmlReader xr = XmlReader.Create(s);
            xr.ReadStartElement("cfgxml");           
            xr.MoveToContent();
            xr.ReadStartElement("section");
            while (xr.MoveToContent() == XmlNodeType.Element)
            {
                ProcessElement(xr);
            }
        }

        static void ProcessElement(XmlReader xr)
        {
            if (xr.EOF)
                return;

            string name = xr.GetAttribute("name");
            
            switch (name)
            {
                case "inputpath":
                    inputPath.AddRange(ProcessPathList(xr));
                    break;
                case "libs":
                    libs = ProcessStringList(xr);
                    break;
                case "rununittests":
                    runUnitTests = xr.ReadElementContentAsBoolean();
                    break;
                case "outputgrammar":
                    outputGrammar = xr.ReadElementContentAsBoolean();
                    break;
                case "outputprimitives":
                    outputPrimitives = xr.ReadElementContentAsBoolean();
                    break;
                case "extensions":
                    extensions = ProcessStringList(xr);
                    break;
                case "maxthreads":
                    maxThreads = xr.ReadElementContentAsInt();
                    break;
                case "showtiming":
                    showTiming = xr.ReadElementContentAsBoolean();
                    break;
                case "waitforkeypress":
                    waitForKeypress = xr.ReadElementContentAsBoolean();
                    break;
                case "optimize":
                    optimize = xr.ReadElementContentAsBoolean();
                    break;
                default:
                    throw new Exception("Unrecognized node type '" + name + "'");
            }
        }

        static string ProcessPath(XmlReader xr)
        {
            if (xr.GetAttribute("relative") == "true")
                return Util.GetExeDir() + "\\" + xr.ReadElementContentAsString();
            else
                return xr.ReadElementContentAsString();
        }

        static List<string> ProcessPathList(XmlReader xr)
        {
            xr.ReadStartElement("pathlist");
            List<string> paths = new List<string>();
            XmlNodeType xnt = xr.MoveToContent();
            while (xnt == XmlNodeType.Element)
            {
                paths.Add(ProcessPath(xr));
                xnt = xr.MoveToContent();
            }
            if (xnt != XmlNodeType.EndElement)
                throw new Exception("Config parsing error, expected end element");
            xr.ReadEndElement();                
            return paths;
        }

        static List<string> ProcessStringList(XmlReader xr)
        {
            xr.ReadStartElement("stringlist");
            List<string> r = new List<string>();
            XmlNodeType xnt = xr.MoveToContent();
            while (xnt == XmlNodeType.Element)
            {
                r.Add(xr.ReadElementContentAsString());
                xnt = xr.MoveToContent();
            }
            if (xnt != XmlNodeType.EndElement)
                throw new Exception("Config parsing error, expected end element");
            xr.ReadEndElement();                
            return r;
        }
    }
}
