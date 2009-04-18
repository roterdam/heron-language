using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using HeronEngine;
using Peg;
using Util;

namespace HeronTests
{
    public class HeronTests
    {
        static HeronExecutor vm = new HeronExecutor();

        static public void TestPeg(Grammar.Rule r, string s)
        {
            try
            {
                Console.WriteLine("Trying to parse input " + s);
                AstNode node = ParserState.Parse(r, s);
                if (node.GetNumChildren() != 1)
                    throw new Exception("Test failed, more than one child node parsed");
                node = node.GetChild(0);
                if (node == null)
                    Console.WriteLine("Test failed");
                else
                    Console.WriteLine("Test succeed, node = " + node.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Test failed with exception: " + e.Message);
            }
        }

        static public void TestExpr(string s)
        {
            Console.WriteLine("testing expression: " + s);
            try
            {
                Expr x = HeronExecutor.ParseExpr(s);
                if (x != null)
                {
                    Console.WriteLine("test passed");
                    Console.WriteLine("result string = " + x.ToString() + ", type " + x.GetType().ToString());
                }
                else
                    Console.WriteLine("test failed without exception");
            }
            catch (Exception e)
            {
                Console.WriteLine("test failed with exception " + e.Message);
            }
        }

        static public void TestStatement(string s)
        {
            Console.WriteLine("testing statement: " + s);
            try
            {
                Statement x = HeronExecutor.ParseStatement(s);
                if (x != null)
                    Console.WriteLine("test passed");
                else
                    Console.WriteLine("test failed without exception");
            }
            catch (Exception e)
            {
                Console.WriteLine("test failed with exception " + e.Message);
            }
        }

        static public void TestEvalExpr(string sExpr, string sOuput)
        {
            Console.WriteLine("testing evaluation of " + sExpr);
            Console.WriteLine("expecting result of " + sOuput);
            try
            {
                HeronObject o = vm.EvalExpr(sExpr);
                Console.WriteLine("test result was " + o.ToString());

            }
            catch (Exception e)
            {
                Console.WriteLine("test failed with exception " + e.Message);
            }
        }

        static public void RunFileTest(string file)
        {
            Console.WriteLine("Loading and evaluating file " + file);
            string sModule = Util.Util.ReadFromFile(file);
            try
            {
                vm.EvalModule(sModule);
            }
            catch (ParsingException e)
            {
                Console.WriteLine("Parsing exception occured in file " + file);
                Console.WriteLine("at character " + e.col + " of line " + e.row);
                Console.WriteLine("while parsing rule " + e.rule.ToString());
                Console.WriteLine(e.line);
                Console.WriteLine(e.ptr);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occured when parsing file " + file);
                Console.WriteLine(e.Message);
            }
               
        }

        static public void RunAllTestFiles()
        {
            int n = 1;
            while (true)
            {
                string s = "test" + n.ToString() + ".heron";
                if (!File.Exists(s))
                    return;
                RunFileTest(s);
                n += 1;
            }
        }

        static public void MainTest()
        {
            SimplePegTests();
            SimpleExprTests();
            SimpleEvalExprTests();
            RunAllTestFiles();
            //RunFileTest("SeekingDemo.heron");
            Console.WriteLine("\nPress any key to continue ...");
            Console.ReadKey();
        }

        static void SimpleEvalExprTests()
        {
            TestEvalExpr("1", "1");
            TestEvalExpr("(1)", "1");
            TestEvalExpr("1 + 2", "3");
            TestEvalExpr("(1 + 2)", "3");
            TestEvalExpr("4 / 2", "2");
            TestEvalExpr("4 % 2", "0");
            TestEvalExpr("4 % 3", "1");
            TestEvalExpr("2 * 3 + 2", "8");
            TestEvalExpr("2 * (3 + 2)", "10");
            TestEvalExpr("2 * (3 - 2)", "2");
            TestEvalExpr("1 < 2", "True");
            TestEvalExpr("1 <= 2", "True");
            TestEvalExpr("1 >= 2", "False");
            TestEvalExpr("1 == 1", "True");
            TestEvalExpr("1 != 1", "False");
            TestEvalExpr("1 == 2", "False");
            TestEvalExpr("1 != 2", "True");
            TestEvalExpr("1.0 < 2.3", "True");
            TestEvalExpr("180.23 <= 2.34203", "False");
            TestEvalExpr("1 >= 2", "False");
            TestEvalExpr("1 == 1", "True");
            TestEvalExpr("1.0 != 1.0", "False");
            TestEvalExpr("1.123 == 0.2", "False");
            TestEvalExpr("1 != 2", "True");
            TestEvalExpr("1.0 + 2.5", "3.5");
        }

        static void SimplePegTests()
        {
            TestPeg(HeronGrammar.IntegerLiteral(), "1");
            TestPeg(HeronGrammar.NumLiteral(), "1");
            TestPeg(HeronGrammar.Literal(), "1");
            TestPeg(HeronGrammar.SimpleExpr(), "1");
            TestPeg(HeronGrammar.Expr(), "1");
            TestPeg(HeronGrammar.Expr(), "12");
            TestPeg(HeronGrammar.Expr(), "1.0");
            TestPeg(HeronGrammar.Expr(), "abc");
            TestPeg(HeronGrammar.Expr(), "a + b");
        }

        static void SimpleExprTests()
        {
            TestExpr("1");
            TestExpr("123");
            TestExpr("1.0");
            TestExpr("abc");
            TestExpr("a");
            TestExpr("(1)");
            TestExpr("1 + 2");
            TestExpr("(1 + 2)");
            TestExpr("-35");
            TestExpr("a.f");
            TestExpr("a = 5");
            TestExpr("a.f = 5");
            TestExpr("a[1]");
            TestExpr("f()");
            TestExpr("f(1)");
            TestExpr("f(1,2)");
            TestExpr("f ( 1 , 2 )");
            TestExpr("f ( 1 , 2 )");
            TestExpr("f() + 5");
            TestExpr("1 + (2 * 3)");
            TestExpr("a == 12");
            TestExpr("a != 12");
            TestExpr("a < 12");
            TestExpr("a > 12");
            TestExpr("4 + 12 * 3");
            TestExpr("(4 + 12) * 3");
            TestExpr("a == 3 || b == 4");
        }
    }
}
