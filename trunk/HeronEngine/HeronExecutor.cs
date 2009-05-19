using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Peg;
using HeronTests;

namespace HeronEngine
{
    /// <summary>
    /// Encapsulates the following components:
    /// - concrete syntax tree parser (Parser)
    /// - abstract syntax tree parser (HeronParser)
    /// - lexical environment (Environment)
    /// - evaluator (Expression and Statement)
    /// </summary>
    public class HeronExecutor
    {
        Environment env = new Environment();
        Module module; 

        public HeronExecutor()
        {
        }

        #region evaluation functions
        public HeronObject EvalExpr(string s)
        {
            Expression x = ParseExpr(s);
            HeronObject o = x.Eval(env);
            return o;
        }

        public void EvalModule(string sModule)
        {
            Module m = ParseModule(sModule);
            EvalModule(m);
        }

        public void InitializeEnvironment()
        {
            env.Clear();
            foreach (HeronClass c in module.classes)
                env.AddModuleVar(c.name, c);
        }

        public void EvalModule(Module m)
        {
            module = m;
            InitializeEnvironment();
            HeronClass main = m.GetMainClass();
            if (main == null)
                throw new Exception("Could not evaluate module " 
                    + m.name + " without a class named Main");

            HeronObject inst = main.Instantiate(env, new HeronObject[] { });            
        }

        public void PrecompileModule(Module m)
        {
            module = m;
            InitializeEnvironment();
            HeronClass premain = m.GetPremainClass();
            if (premain == null)
                return;

            HeronObject inst = premain.Instantiate(env, new HeronObject[] { });
        }
        #endregion

        #region static public functions
        static public Expression ParseExpr(string s)
        {
            AstNode node = ParserState.Parse(HeronGrammar.Expr(), s);
            if (node.GetLabel() != "ast")
                throw new Exception("no root AST node");
            if (node.GetNumChildren() != 1)
                throw new Exception("more than one child node parsed");
            node = node.GetChild(0);
            if (node == null)
                return null;
            Expression r = HeronParser.CreateExpr(node);
            return r;
        }

        static public Statement ParseStatement(string s)
        {
            AstNode node = ParserState.Parse(HeronGrammar.Statement(), s);
            if (node.GetLabel() != "ast")
                throw new Exception("no root AST node");
            if (node.GetNumChildren() != 1)
                throw new Exception("more than one child node parsed");
            node = node.GetChild(0);
            if (node == null)
                return null;
            Statement r = HeronParser.CreateStatement(node);
            return r;
        }

        static public Module ParseModule(string s)
        {
            AstNode node = ParserState.Parse(HeronGrammar.Module(), s);
            if (node.GetLabel() != "ast")
                throw new Exception("no root AST node");
            if (node.GetNumChildren() != 1)
                throw new Exception("more than one child node parsed");
            node = node.GetChild(0);
            if (node == null)
                return null;
            Module r = HeronParser.CreateModule(node);
            return r;
        }
        #endregion 

        public Environment GetEnv()
        {
            return env;
        }

        public Module GetModule()
        {
            return module;
        }
    }
}
