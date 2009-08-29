using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    public static class Program
    {
        static public void Usage()
        {
            Console.WriteLine("HeronEngine.exe");
            Console.WriteLine("    by Christopher Diggins");
            Console.WriteLine("    version 0.5");
            Console.WriteLine("    August 11, 2009");
            Console.WriteLine("");
            Console.WriteLine("An execution engine for the Heron language.");
            Console.WriteLine("This program tests the Heron language, but is");
            Console.WriteLine("intended to be used as a class library.");
            Console.WriteLine("");
            Console.WriteLine("Usage: ");
            Console.WriteLine("  HeronEngine <configfile.xml>");
            Console.WriteLine("");
        }

        /// <summary>
        /// Entry point for the application. 
        /// </summary>
        /// <param name="args"></param>
        static public void Main(string[] args)
        {
            if (args.Length != 1) {
                Usage();
                Console.ReadKey();
                return;
            }
            try
            {
                Config.LoadFromFile(args[0]);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occured: " + e.Message);
                Console.WriteLine();
                Usage();
            }
            HeronTests.HeronTests.MainTest();
        }
    }
}
