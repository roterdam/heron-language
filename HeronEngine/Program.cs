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
            Console.WriteLine("");
            Console.WriteLine("Tests the HeronEngine class library");
            Console.WriteLine("");
            Console.WriteLine("Usage: ");
            Console.WriteLine("  HeronEngine <librarypath>");
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
            Config.libraryPath = args[0];
            HeronTests.HeronTests.MainTest();
        }
    }
}
