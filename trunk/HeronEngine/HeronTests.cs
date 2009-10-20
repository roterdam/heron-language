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

using HeronEngine;
using Peg;
using Util;

namespace HeronTests
{
    public class HeronTests
    {
        static HeronExecutor vm = new HeronExecutor();

        static public void TestPeg(Rule r, string s)
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

        static public void TestExprParse(string s)
        {
            Console.WriteLine("testing expression: " + s);
            try
            {
                Expression x = HeronTypedAST.ParseExpr(s);
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
                Statement x = HeronTypedAST.ParseStatement(s);
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

        static public void TestEvalExpr(string sExpr)
        {
            TestEvalExpr(sExpr, "True");
        }
        
        static public void TestEvalExpr(string sExpr, string sOutput)
        {
            Console.WriteLine("testing evaluation of " + sExpr);
            Console.WriteLine("expecting result of " + sOutput);
            try
            {
                HeronValue o = vm.EvalString(sExpr);
                Console.WriteLine("test result was " + o.ToString());
                if (o.ToString() != sOutput)
                    throw new Exception("Result " + o.ToString() + " is different from expected " + sOutput);

            }
            catch (Exception e)
            {
                Console.WriteLine("test failed with exception " + e.Message);
            }
        }

        static public void RunFileTest(string file)
        {
            Console.WriteLine("Loading and evaluating file " + file);
            Program.RunFile(file);
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
                //if (n == 16)
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
            TestEvalExpr("(function() { return 1; })()", "1");
            TestEvalExpr("(function(x : Int) { return x + 1; })(12)", "13");
            TestEvalExpr("null == null");
            TestEvalExpr("null != new String()");
            TestEvalExpr("null != \"abc\"");
            TestEvalExpr("\"abc\" != null");
            TestEvalExpr("null != 1");
            TestEvalExpr("1 != null");
        }

        static void SimplePegTests()
        {
            TestPeg(HeronGrammar.IntegerLiteral, "1");
            TestPeg(HeronGrammar.NumLiteral, "1");
            TestPeg(HeronGrammar.Literal, "1");
            TestPeg(HeronGrammar.BasicExpr, "1");
            TestPeg(HeronGrammar.Name, "a");
            TestPeg(HeronGrammar.Name, "abc");
            TestPeg(HeronGrammar.Name, "*");
            TestPeg(HeronGrammar.Expr, "1");
            TestPeg(HeronGrammar.Expr, "12");
            TestPeg(HeronGrammar.Expr, "1.0");
            TestPeg(HeronGrammar.Expr, "abc");
            TestPeg(HeronGrammar.Expr, "a + b");
            TestPeg(HeronGrammar.ParanthesizedExpr, "(1)");
            TestPeg(HeronGrammar.ParanthesizedExpr, "(1 + 2)");
            TestPeg(HeronGrammar.Expr, "(1 + 2)");
            TestPeg(HeronGrammar.Expr, "(1 + 2) * (3 + 4)");
            TestPeg(HeronGrammar.Expr, "ab");
            TestPeg(HeronGrammar.Expr, "ab(a)");
            TestPeg(HeronGrammar.Expr, "a.x");
            TestPeg(HeronGrammar.Expr, "a.x");
            TestPeg(HeronGrammar.Expr, "ab(a.x + 24)");
            TestPeg(HeronGrammar.Expr, "function() { }");
            TestPeg(HeronGrammar.Expr, "function(a : Int) { return a + 1; }");
            TestPeg(HeronGrammar.Expr, "f(function(a : Int) { return a + 1; })");
        }

        static void SimplePegStatementTests()
        {
            TestPeg(HeronGrammar.ExprStatement, "1;");
            TestPeg(HeronGrammar.ExprStatement, "f;");
            TestPeg(HeronGrammar.ExprStatement, "a.b;");
            TestPeg(HeronGrammar.ExprStatement, "a.b;");
            TestPeg(HeronGrammar.CodeBlock, "{}");
            TestPeg(HeronGrammar.CodeBlock, "{ }");
            TestPeg(HeronGrammar.CodeBlock, "{ a; }");
            TestPeg(HeronGrammar.VarDecl, "var a;");
            TestPeg(HeronGrammar.VarDecl, "var a : Int;");
            TestPeg(HeronGrammar.IfStatement, "if (a) { }");
            TestPeg(HeronGrammar.IfStatement, "if (a) { } else { }");
            TestPeg(HeronGrammar.ForEachStatement, "foreach (a in b) { }");
            TestPeg(HeronGrammar.ForEachStatement, "foreach (a : A in b) { }");
            TestPeg(HeronGrammar.ForStatement, "for (a = 0; a != b; a + 1) { }");
            TestPeg(HeronGrammar.WhileStatement, "while (a) { }");
            TestPeg(HeronGrammar.EmptyStatement, ";");
            TestPeg(HeronGrammar.ReturnStatement, "return a;");
            TestPeg(HeronGrammar.DeleteStatement, "delete a;");
            TestPeg(HeronGrammar.SwitchStatement, "switch (a) { case (b) { } default { } }");
            TestPeg(HeronGrammar.Statement, "f(function() { return a + 1; });");
            TestPeg(HeronGrammar.Statement, "f(function(a : Int) { a += 1; return a * 2; });");
        }

        static void SimpleExprTests()
        {
            TestExprParse("1");
            TestExprParse("123");
            TestExprParse("1.0");
            TestExprParse("abc");
            TestExprParse("a");
            TestExprParse("(1)");
            TestExprParse("1 + 2");
            TestExprParse("(1 + 2)");
            TestExprParse("-35");
            TestExprParse("a.f");
            TestExprParse("a = 5");
            TestExprParse("a.f = 5");
            TestExprParse("a[1]");
            TestExprParse("f()");
            TestExprParse("f(1)");
            TestExprParse("f(1,2)");
            TestExprParse("f ( 1 , 2 )");
            TestExprParse("f ( 1 , 2 )");
            TestExprParse("f() + 5");
            TestExprParse("1 + (2 * 3)");
            TestExprParse("a == 12");
            TestExprParse("a != 12");
            TestExprParse("a < 12");
            TestExprParse("a > 12");
            TestExprParse("4 + 12 * 3");
            TestExprParse("(4 + 12) * 3");
            TestExprParse("a == 3 || b == 4");
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
        }
    }
}

