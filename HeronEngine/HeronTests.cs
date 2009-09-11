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
                Expression x = HeronParser.ParseExpr(s);
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
                Statement x = HeronParser.ParseStatement(s);
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
                Console.WriteLine("at character " + e.context.col + " of line " + e.context.row);
                //if (e.rule != null)
                //Console.WriteLine("while parsing rule " + e.rule.ToString());
                Console.WriteLine(e.context.msg);
                Console.WriteLine(e.context.line);
                Console.WriteLine(e.context.ptr);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occured when executing file " + file);
                Console.WriteLine(e.Message);
            }               
        }

        /// <summary>
        /// Iterates over the files in the test directory named: 
        /// test1.heron, test2.heron, etc. until it fails to find 
        /// a consecutively named file.
        /// </summary>
        static public void RunAllTestFiles()
        {
            int n = 1;
            while (true)
            {
                string s = Config.testPath + "\\test" + n.ToString() + ".heron";
                if (!File.Exists(s))
                    return;
                
                // TEMP:
                if (n == 14)
                    RunFileTest(s);

                n += 1;
            }
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
            TestPeg(HeronGrammar.Expr(), "(1 + 2) * (3 + 4)");
        }

        static void SimplePegStatementTests()
        {
            TestPeg(HeronGrammar.ExprStatement(), "1;");
            TestPeg(HeronGrammar.ExprStatement(), "f();");
            TestPeg(HeronGrammar.ExprStatement(), "a.b;");
            TestPeg(HeronGrammar.ExprStatement(), "a.b();");
            TestPeg(HeronGrammar.CodeBlock(), "{}");
            TestPeg(HeronGrammar.CodeBlock(), "{ }");
            TestPeg(HeronGrammar.CodeBlock(), "{ a(); }");
            TestPeg(HeronGrammar.VarDecl(), "var a;");
            TestPeg(HeronGrammar.VarDecl(), "var a : Int;");
            TestPeg(HeronGrammar.IfStatement(), "if (a) { }");
            TestPeg(HeronGrammar.IfStatement(), "if (a) { } else { }");
            TestPeg(HeronGrammar.ForEachStatement(), "foreach (a in b) { }");
            TestPeg(HeronGrammar.ForEachStatement(), "foreach (a : A in b) { }");
            TestPeg(HeronGrammar.ForStatement(), "for (a = 0; a != b; a + 1) { }");
            TestPeg(HeronGrammar.WhileStatement(), "foreach (a in b) { }");
            TestPeg(HeronGrammar.EmptyStatement(), "while (a) { }");
            TestPeg(HeronGrammar.ReturnStatement(), "return a;");
            TestPeg(HeronGrammar.DeleteStatement(), "delete a;");
            TestPeg(HeronGrammar.SwitchStatement(), "switch (a) { case (b) { } default { } }");
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

        static public void MainTest()
        {
            if (Config.runUnitTests)
            {
                SimplePegTests();
                SimplePegStatementTests();
                SimpleExprTests();
                SimpleEvalExprTests();
            }

            if (Config.runTestFiles)
            {
                RunAllTestFiles();
            }

             /*
            RunFileTest(@"C:\Users\Chr15topher\AppData\Roaming\Heron\SeekingDemoPackage.heron");
             */
            Console.WriteLine("\nPress any key to continue ...");
            Console.ReadKey();
        }
    }
}

