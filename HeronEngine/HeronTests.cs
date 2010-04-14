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
    public class HeronTests
    {
        static VM vm = new VM();

        static HeronTests()
        {
            vm.InitializeVM();
        }

        static public void TestPeg(Rule r, string s)
        {
            try
            {
                Console.WriteLine("Trying to parse input " + s);
                ParseNode node = ParserState.Parse(r, s);
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

        static public void TestCreateExprParse(string s)
        {
            Console.WriteLine("testing expression: " + s);
            try
            {
                Expression x = CodeModelBuilder.ParseExpr(vm.Program, s);
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
                Statement x = CodeModelBuilder.ParseStatement(vm.Program, s);
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

        static public void TestCompareValues(string sExpr)
        {
            TestCompareValues(sExpr, "true");
        }
        
        static public void TestCompareValues(string sInput, string sOutput)
        {
            Console.WriteLine("testing evaluation of " + sInput);
            Console.WriteLine("expecting result of " + sOutput);
            try
            {
                HeronValue input = vm.EvalString(sInput);
                HeronValue output = vm.EvalString(sOutput);
                Console.WriteLine("test input was " + input.ToString());
                Console.WriteLine("test output was " + output.ToString());
                if (!input.Equals(output))
                    throw new Exception("Result was different than expected");
            }
            catch (Exception e)
            {
                Console.WriteLine("test failed with exception " + e.Message);
            }
        }

        static void EvalExprTests()
        {
            TestCompareValues("1", "1");
            TestCompareValues("(1)", "1");
            TestCompareValues("1 + 2", "3");
            TestCompareValues("(1 + 2)", "3");
            TestCompareValues("4 / 2", "2");
            TestCompareValues("4 % 2", "0");
            TestCompareValues("4 % 3", "1");
            TestCompareValues("2 * 3 + 2", "8");
            TestCompareValues("2 * (3 + 2)", "10");
            TestCompareValues("2 * (3 - 2)", "2");
            TestCompareValues("1 < 2", "true");
            TestCompareValues("1 <= 2", "true");
            TestCompareValues("1 >= 2", "false");
            TestCompareValues("1 == 1", "true");
            TestCompareValues("1 != 1", "false");
            TestCompareValues("1 == 2", "false");
            TestCompareValues("1 != 2", "true");
            TestCompareValues("1.0 < 2.3", "true");
            TestCompareValues("180.23 <= 2.34203", "false");
            TestCompareValues("1 >= 2", "false");
            TestCompareValues("1 == 1", "true");
            TestCompareValues("1.0 != 1.0", "false");
            TestCompareValues("1.123 == 0.2", "false");
            TestCompareValues("1 != 2", "true");
            TestCompareValues("1.0 + 2.5", "3.5");
            TestCompareValues("(function() { return 1; })()", "1");
            TestCompareValues("(function(x : Int) { return x + 1; })(12)", "13");
            TestCompareValues("null == null");
            TestCompareValues("null != new String()");
            TestCompareValues("null != \"abc\"");
            TestCompareValues("\"abc\" != null");
            TestCompareValues("null != 1");
            TestCompareValues("1 != null");
        }

        static void EvalPrimitiveMethodTests()
        {
            TestCompareValues("\"abc\".Length()", "3");
            TestCompareValues("\"abc\".GetChar(1)", "'b'");
            TestCompareValues("1.AsString()", "\"1\"");
        }

        static void EvalListTests()
        {
            TestCompareValues("[1, 2, 3].Count()", "3");
            TestCompareValues("0..2", "0..2");
            TestCompareValues("[0, 1, 2]", "[0, 1, 2]");
            TestCompareValues("0..2", "[0, 1, 2]");
            TestCompareValues("map (i in 0..2) i * 2", "[0, 2, 4]");
            TestCompareValues("select (i from 0..5) i % 2 == 1", "[1, 3, 5]");
            TestCompareValues("accumulate (a = 0 forall i in 0..3) a + i", "6");
            TestCompareValues("reduce (a, b in 0..3) a + b", "[6]");
        }

        static void PegTests()
        {
            TestPeg(HeronGrammar.IntegerLiteral, "1");
            TestPeg(HeronGrammar.NumLiteral, "1");
            TestPeg(HeronGrammar.Literal, "1");
            TestPeg(HeronGrammar.BasicExpr, "1");
            TestPeg(HeronGrammar.Name, "a");
            TestPeg(HeronGrammar.Name, "abc");
            TestPeg(HeronGrammar.Name, "*");
            TestPeg(HeronGrammar.CompoundExpr, "1");
            TestPeg(HeronGrammar.CompoundExpr, "12");
            TestPeg(HeronGrammar.CompoundExpr, "1.0");
            TestPeg(HeronGrammar.CompoundExpr, "abc");
            TestPeg(HeronGrammar.CompoundExpr, "a + b");
            TestPeg(HeronGrammar.ParanthesizedExpr, "(1)");
            TestPeg(HeronGrammar.ParanthesizedExpr, "(1 + 2)");
            TestPeg(HeronGrammar.CompoundExpr, "(1 + 2)");
            TestPeg(HeronGrammar.CompoundExpr, "(1 + 2) * (3 + 4)");
            TestPeg(HeronGrammar.CompoundExpr, "ab");
            TestPeg(HeronGrammar.CompoundExpr, "ab(a)");
            TestPeg(HeronGrammar.CompoundExpr, "a.x");
            TestPeg(HeronGrammar.CompoundExpr, "a.f");
            TestPeg(HeronGrammar.CompoundExpr, "a[1]");
            TestPeg(HeronGrammar.CompoundExpr, "a[1,2]");
            TestPeg(HeronGrammar.CompoundExpr, "a['b']");
            TestPeg(HeronGrammar.CompoundExpr, "a[\"hello\"]");
            TestPeg(HeronGrammar.CompoundExpr, "a && b");
            TestPeg(HeronGrammar.CompoundExpr, "a .. b");
            TestPeg(HeronGrammar.CompoundExpr, "[a, b, c]");
            TestPeg(HeronGrammar.CompoundExpr, "ab(a.x + 24)");
            TestPeg(HeronGrammar.CompoundExpr, "function() { }");
            TestPeg(HeronGrammar.CompoundExpr, "function(a : Int) { return a + 1; }");
            TestPeg(HeronGrammar.CompoundExpr, "f(function(a : Int) { return a + 1; })");
            TestPeg(HeronGrammar.SelectExpr, "select (a from b..c) a % 2 == 1");
            TestPeg(HeronGrammar.MapExpr, "map (a in b..c) a * 2");
            TestPeg(HeronGrammar.AccumulateExpr, "accumulate (a = 0 forall b in c..d) a + b");
            TestPeg(HeronGrammar.SpecialName, "null");
            TestPeg(HeronGrammar.SpecialName, "true");
            TestPeg(HeronGrammar.SpecialName, "false");
        }

        static void PegStatementTests()
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

        static void ExprParseTests()
        {
            TestCreateExprParse("null");
            TestCreateExprParse("a.f");
            TestCreateExprParse("1");
            TestCreateExprParse("123");
            TestCreateExprParse("1.0");
            TestCreateExprParse("abc");
            TestCreateExprParse("a");
            TestCreateExprParse("(1)");
            TestCreateExprParse("1 + 2");
            TestCreateExprParse("(1 + 2)");
            TestCreateExprParse("-35");
            TestCreateExprParse("a = 5");
            TestCreateExprParse("a.f = 5");
            TestCreateExprParse("a[1]");
            TestCreateExprParse("f()");
            TestCreateExprParse("f(1)");
            TestCreateExprParse("f(1,2)");
            TestCreateExprParse("f ( 1 , 2 )");
            TestCreateExprParse("f ( 1 , 2 )");
            TestCreateExprParse("f() + 5");
            TestCreateExprParse("1 + (2 * 3)");
            TestCreateExprParse("a == 12");
            TestCreateExprParse("a != 12");
            TestCreateExprParse("a < 12");
            TestCreateExprParse("a > 12");
            TestCreateExprParse("4 + 12 * 3");
            TestCreateExprParse("(4 + 12) * 3");
            TestCreateExprParse("a == 3 || b == 4");
        }

        static public void MainTest()
        {
            PegTests();
            PegStatementTests();
            ExprParseTests();
            EvalExprTests();
            EvalPrimitiveMethodTests(); 
            EvalListTests();
        }
    }
}
