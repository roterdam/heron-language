using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    public abstract class Statement 
    {
        public abstract void Execute(Environment env);

        public void Trace()
        {
            HeronDebugger.TraceStatement(this);
        }
    }

    public class VarDecl : Statement
    {
        public string name;
        public string type;
        public Expr init;

        public override void Execute(Environment env)
        {
            Trace();
            HeronObject initVal = init.Eval(env);
            env.AddVar(name, initVal);
        }

        public override string ToString()
        {
            string r = "var " + name + " : " + type;
            if (init != null)
                r += " = " + init.ToString();
            return r;
        }
    }

    public class DeleteStatement : Statement
    {
        public Expr expr;

        public override void Execute(Environment env)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return "delete " + expr.ToString();
        }
    }

    public class ExprStatement : Statement
    {
        public Expr expr;

        public override void Execute(Environment env)
        {
            Trace();
            expr.Eval(env);
        }

        public override string ToString()
        {
            return expr.ToString();
        }
    }

    public class ForEach : Statement
    {
        public string name;
        public Expr coll;
        public Statement body;

        public override void Execute(Environment env)
        {
            // TODO: make this exception safe. Have a "using" with a special scope 
            // construction object.
            env.PushScope();
            env.AddVar(name, null);
            HeronObject c = this.coll.Eval(env);
            if (!(c is DotNetObject))
                throw new Exception("Unable to iterate over " + coll.ToString() + " because it is not a collection");

            DotNetObject tmp = c as DotNetObject;
            Object o = tmp.ToSystemObject();
            if (!(o is HeronCollection))
                throw new Exception("Unable to iterate over " + coll.ToString() + " because it is not a collection");
            IEnumerable<Object> list = (o as HeronCollection).InternalGetList(); 

            foreach (HeronObject ho in list) {
                env.SetVar(name, ho);
                body.Execute(env);
            }
            env.PopScope();
        }

        public override string ToString()
        {
            return "foreach (" + name + " in " + coll.ToString() + ")\n" 
                + body.ToString();
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
            Trace();
            HeronObject initVal = init.Eval(env);
            env.AddVar(name, initVal);
            while (true)
            {
                HeronObject condVal = cond.Eval(env);
                bool b = condVal.ToBool();
                if (!b)
                    break;
                body.Execute(env);
                next.Eval(env);
            }
        }

        public override string ToString()
        {
            return "for (" + name 
                + " = " + init.ToString() 
                + "; " + cond.ToString() 
                + "; " + next.ToString() 
                + ")\n" + body.ToString();
        }
    }

    public class CodeBlock : Statement
    {
        public List<Statement> statements = new List<Statement>();
        
        public override void Execute(Environment env)
        {
            Trace();
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
            Trace();
            bool b = cond.Eval(env).ToBool();
            if (b)
                ontrue.Execute(env); 
            else
                if (onfalse != null)
                    onfalse.Execute(env);
        }
    }

    public class While : Statement
    {
        public Expr cond;
        public Statement body;

        public override void Execute(Environment env)
        {
            Trace();
            while (true)
            {
                HeronObject o = cond.Eval(env);
                bool b = o.ToBool();
                if (!b)
                    break;
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
            Trace();
            env.Return(expr.Eval(env));
        }
    }
}
