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

using Peg;

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
            Console.WriteLine("    version 0.7");
            Console.WriteLine("    October 18, 2009");
            Console.WriteLine("");
            Console.WriteLine("An execution engine for the Heron language.");
            Console.WriteLine("This program tests the Heron language, but is");
            Console.WriteLine("intended to be used as a class library.");
            Console.WriteLine("");
            Console.WriteLine("Usage: ");
            Console.WriteLine("  HeronEngine.exe"); 
            Console.WriteLine("    -c configfile.xml   Specifies configuration file to use");
            Console.WriteLine("    -r inputfile.heron  Specifies input file to run");
            Console.WriteLine("");
        }

        /// <summary>
        /// Loads, parses, and executes a file
        /// </summary>
        /// <param name="s"></param>
        static public void RunFile(string file)
        {
            HeronExecutor vm = new HeronExecutor();
            string sModule = Util.Util.ReadFromFile(file);
            try
            {
                vm.EvalModule(sModule);
            }
            catch (ParsingException e)
            {
                Console.WriteLine("Parsing exception occured in file " + file);
                Console.WriteLine("at character " + e.context.col + " of line " + e.context.row);
                Console.WriteLine(e.context.msg);
                Console.WriteLine(e.context.line);
                Console.WriteLine(e.context.ptr);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occured when executing file " + file);
                Console.WriteLine(e.Message);
                HeronDebugger.Start(vm);
            }
        }
        
        /// <summary>
        /// Load the config file if specified in the command-line argument list
        /// </summary>
        /// <param name="args"></param>
        static public void LoadConfig(string[] args)
        {
            for (int i = 0; i < args.Length - 1; ++i)
            {
                if (args[i] == "-c" || args[i] == "/c")
                {
                    string config = args[i + 1];
                    Config.LoadFromFile(config);
                }
            }
        }

        /// <summary>
        /// Run a file if specified in the command-line argument list
        /// </summary>
        /// <param name="args"></param>
        static public void RunFile(string[] args)
        {
            for (int i = 0; i < args.Length - 1; ++i)
            {
                if (args[i] == "-x" || args[i] == "/x")
                {
                    string input = args[i + 1];

                    if (!File.Exists(input))
                        throw new Exception("Could not open file: " + args[1]);

                    RunFile(input);
                }
            }
        }

        /// <summary>
        /// Entry point for the application. 
        /// </summary>
        /// <param name="args"></param>
        static public void Main(string[] args)
        {
            System.Environment.SpecialFolder.SendTo();
            if (args.Length < 1) 
            {
                Usage();
                Console.ReadKey();
                return;
            }
            try
            {
                LoadConfig(args);
                HeronTests.HeronTests.MainTest();
                RunFile(args);
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
