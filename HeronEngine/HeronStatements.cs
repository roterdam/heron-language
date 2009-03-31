using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    public abstract class Statement : HObject 
    {
        public abstract void Execute(Environment env);
    }

    public class VarDecl : Statement
    {
        public string name;
        public string type;
        public Expr init;

        public override void Execute(Environment env)
        {
            env.AddVar(name, null);
        }
    }

    public class DeleteStatement : Statement
    {
        public Expr expr;

        public override void Execute(Environment env)
        {
            throw new NotImplementedException();
        }
    }

    public class ExprStatement : Statement
    {
        public Expr expr;

        public override void Execute(Environment env)
        {
            expr.Eval(env);
        }
    }

    public class ForEach : Statement
    {
        public string name;
        public Expr coll;
        public Statement body;

        public override void Execute(Environment env)
        {
            throw new NotImplementedException();
        }
    }

    public class For : Statement
    {
        public string name;
        public Expr init;
        public Expr cond;
        public Expr next;
        public Statement body;

        public override void Execute(Environment env)
        {
            throw new NotImplementedException();
        }
    }

    public class CodeBlock : Statement
    {
        public List<Statement> statements = new List<Statement>();
        
        public override void Execute(Environment env)
        {
            env.PushScope();
            foreach (Statement s in statements)
            {
                s.Execute(env);
                if (env.ShouldExitScope())
                    break;
            }
            env.PopScope();
        }
    }

    public class If : Statement
    {
        public Expr cond;
        public Statement ontrue;
        public Statement onfalse;


        public override void Execute(Environment env)
        {
            bool b = cond.Eval(env).ToBool();
            if (b)
                ontrue.Execute(env); else
                onfalse.Execute(env);
        }
    }

    public class While : Statement
    {
        public Expr cond;
        public Statement body;

        public override void Execute(Environment env)
        {
            while (cond.Eval(env).ToBool())
            {
                body.Execute(env);
                if (env.ShouldExitScope())
                    break;
            }
        }
    }

    public class Return : Statement
    {
        public Expr expr;

        public override void Execute(Environment env)
        {
            env.Return(expr.Eval(env));
        }
    }
}
