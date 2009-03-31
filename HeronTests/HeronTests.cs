using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using HeronEngine;
using Peg;

namespace HeronTests
{
    public class Util
    {
        static public void TestPeg(Grammar.Rule r, string s)
        {
            try 
            {
                Console.WriteLine("Trying to parse input " + s);
                AstNode node = Parser.Parse(r, s);
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

        static public Expr ParseExpr(string s)
        {
            AstNode node = Parser.Parse(HeronGrammar.Expr(), s);
            if (node.GetLabel() != "ast")
                throw new Exception("Test failed, no root AST node ");
            if (node.GetNumChildren() != 1)
                throw new Exception("Test failed, more than one child node parsed");
            node = node.GetChild(0);
            if (node == null)
                return null;
            Expr r = HeronParser.CreateExpr(node);
            return r;
        }

        static public Statement ParseStatement(string s)
        {
            AstNode node = Parser.Parse(HeronGrammar.Statement(), s);
            if (node.GetNumChildren() != 1)
                throw new Exception("Test failed, more than one child node parsed");
            node = node.GetChild(0);
            if (node == null)
                return null;
            Statement r = HeronParser.CreateStatement(node);
            return r;
        }

        static public void TestExpr(string s) 
        {
            Console.WriteLine("testing expression: " + s);
            try
            {
                Expr x = ParseExpr(s);
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
                Statement x = ParseStatement(s);
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
                Expr x = ParseExpr(sExpr);
                HObject o = x.Eval(new HeronEngine.Environment());
                Console.WriteLine("test result was " + o.ToString());

            }
            catch (Exception e)
            {
                Console.WriteLine("test failed with exception " + e.Message);                
            }
        }
    }

    public class HeronTests
    {
        static public void Main(string[] args)
        {
            SimplePegTests();
            SimpleExprTests();
            SimpleEvalExprTests();
            Console.WriteLine("\nPress any key to continue ...");
            Console.ReadKey();
        }

        static void SimpleEvalExprTests()
        {
        }

        static void SimplePegTests()
        {
            Util.TestPeg(HeronGrammar.IntegerLiteral(), "1");
            Util.TestPeg(HeronGrammar.NumLiteral(), "1");
            Util.TestPeg(HeronGrammar.Literal(), "1");
            Util.TestPeg(HeronGrammar.SimpleExpr(), "1");
            Util.TestPeg(HeronGrammar.Expr(), "1");
            Util.TestPeg(HeronGrammar.Expr(), "12");
            Util.TestPeg(HeronGrammar.Expr(), "1.0");
            Util.TestPeg(HeronGrammar.Expr(), "abc");
            Util.TestPeg(HeronGrammar.Expr(), "a + b");
        }

        static void SimpleExprTests()
        {
            Util.TestExpr("1");
            Util.TestExpr("123");
            Util.TestExpr("1.0");
            Util.TestExpr("abc");
            Util.TestExpr("a");
            Util.TestExpr("(1)");
            Util.TestExpr("1 + 2");
            Util.TestExpr("(1 + 2)");
            Util.TestExpr("-35");
            Util.TestExpr("a.f");
            Util.TestExpr("a = 5");
            Util.TestExpr("a.f = 5");
            Util.TestExpr("a[1]");
            Util.TestExpr("f()");
            Util.TestExpr("f(1)");
            Util.TestExpr("f(1,2)");
            Util.TestExpr("f ( 1 , 2 )");
            Util.TestExpr("f ( 1 , 2 )");
            Util.TestExpr("f() + 5");
            Util.TestExpr("1 + (2 * 3)");
            Util.TestExpr("a == 12");
            Util.TestExpr("a != 12");
            Util.TestExpr("a < 12");
            Util.TestExpr("a > 12");
            Util.TestExpr("4 + 12 * 3");
            Util.TestExpr("(4 + 12) * 3");
            Util.TestExpr("a == 3 || b == 4");
        }
    }
}
