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

namespace HeronEngine
{
    public static class Program
    {
        /// <summary>
        /// Prints a usage message to the console
        /// </summary>
        static public void Usage()
        {
            Console.WriteLine("HeronEngine.exe");
            Console.WriteLine("    by Christopher Diggins");
            Console.WriteLine("    version 0.8");
            Console.WriteLine("    December 12, 2009");
            Console.WriteLine("");
            Console.WriteLine("An execution engine for the Heron language.");
            Console.WriteLine("This program tests the Heron language, but is");
            Console.WriteLine("intended to be used as a class library.");
            Console.WriteLine("");
            Console.WriteLine("Usage: ");
            Console.WriteLine("  HeronEngine.exe inputfile.heron ");
            Console.WriteLine("");
            Console.WriteLine("The configuration file 'config.xml' is loaded if it is found");
            Console.WriteLine("in the same directory as the Heron executable.");
            Console.WriteLine("");
        }

        /// <summary>
        /// Write the grammar in human readable form to the file "grammar.txt" in the executable directory
        /// </summary>
        static public void OutputGrammar()
        {
            string s = Grammar.ToString(typeof(HeronGrammar));
            File.WriteAllText(Util.GetExeDir() + "\\grammar.txt", s);
        }

        /// <summary>
        /// Outputs all of the primitive types with their fields and methods.
        /// </summary>
        static public void OutputPrimitives()
        {
            string s = PrimitiveTypes.NonCodeModelTypesAsString();
            File.WriteAllText(Util.GetExeDir() + "\\primitives.txt", s);
            s = PrimitiveTypes.CodeModelTypesAsString();
            File.WriteAllText(Util.GetExeDir() + "\\codemodel.txt", s);
        }

        /// <summary>
        /// Loads, parses, and executes a file
        /// </summary>
        /// <param name="s"></param>
        static public void RunFile(string file)
        {
            VM vm = new VM();
            string sFileContents = Util.ReadFromFile(file);
            try
            {
                vm.EvalFile(sFileContents);
            }
            catch (ParsingException e)
            {
                Console.WriteLine("Parsing exception occured in file " + file);
                Console.WriteLine("at character " + e.context.col + " of line " + e.context.row);
                Console.WriteLine(e.context.msg);
                Console.WriteLine(e.context.line);
                Console.WriteLine(e.context.ptr);
            }
            catch (TypedASTException e)
            {
                Console.WriteLine("Error occured during typed parse tree construction in file " + file);
                Console.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occured when executing file " + file);
                Console.WriteLine(e.Message);
                
                // HeronDebugger.Start(vm);
            }
        }
        
        /// <summary>
        /// Load the config file if specified in the command-line argument list
        /// </summary>
        /// <param name="args"></param>
        static public void LoadConfig()
        {
            string configFile = Util.GetExeDir() + "\\config.xml";
            if (File.Exists(configFile))
                Config.LoadFromFile(configFile);
        }

        /// <summary>
        /// Entry point for the application. 
        /// </summary>
        /// <param name="args"></param>
        static public void Main(string[] args)
        {
            if (args.Length != 1) 
            {
                Usage();
                Console.ReadKey();
                return;
            }
            try
            {
                LoadConfig();
                if (Config.outputGrammar) 
                    OutputGrammar();
                if (Config.outputPrimitives) 
                    OutputPrimitives();
                if (Config.runUnitTests) 
                    HeronTests.MainTest();
                RunFile(Util.GetExeDir() + "\\" + args[0]);
                Console.WriteLine("Press any key to exit ...");
                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occured: " + e.Message);
                Console.WriteLine();
                Usage();
            }
        }
    }
}
