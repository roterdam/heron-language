using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    public abstract class Statement 
    {
        public Peg.AstNode node;

        public abstract void Execute(Environment env);

        internal Statement(Peg.AstNode node)
        {
            this.node = node;
        }

        protected void Trace()
        {
            HeronDebugger.TraceStatement(this);
        }

        public abstract string StatementType();
    }

    public class VariableDeclaration : Statement
    {
        public string name;
        public string type;
        public Expression init;

        public VariableDeclaration(Peg.AstNode node)
            : base(node)
        {
        }

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

        public override string StatementType()
        {
            return "variable_declaration";
        }
    }

    public class DeleteStatement : Statement
    {
        public Expression expression;

        internal DeleteStatement(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Execute(Environment env)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return "delete " + expression.ToString();
        }

        public override string StatementType()
        {
            return "delete_statement";
        }
    }

    public class ExpressionStatement : Statement
    {
        public Expression expression;

        internal ExpressionStatement(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Execute(Environment env)
        {
            Trace();
            expression.Eval(env);
        }

        public override string ToString()
        {
            return expression.ToString();
        }

        public override string StatementType()
        {
            return "expression_statement";
        }
    }

    public class ForEach : Statement
    {
        public string name;
        public Expression collection;
        public Statement body;

        internal ForEach(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Execute(Environment env)
        {
            // TODO: make this exception safe. Have a "using" with a special scope 
            // construction object.
            env.PushScope();
            env.AddVar(name, null);
            HeronObject c = this.collection.Eval(env);
            if (!(c is DotNetObject))
                throw new Exception("Unable to iterate over " + collection.ToString() + " because it is not a collection");

            DotNetObject tmp = c as DotNetObject;
            Object o = tmp.ToSystemObject();
            if (!(o is HeronCollection))
                throw new Exception("Unable to iterate over " + collection.ToString() + " because it is not a collection");
            IEnumerable<Object> list = (o as HeronCollection).InternalGetList(); 

            foreach (HeronObject ho in list) {
                env.SetVar(name, ho);
                body.Execute(env);
            }
            env.PopScope();
        }

        public override string ToString()
        {
            return "foreach (" + name + " in " + collection.ToString() + ")\n" 
                + body.ToString();
        }

        public override string StatementType()
        {
            return "foreach_statement";
        }
    }

    public class For : Statement
    {
        public string name;
        public Expression initial;
        public Expression condition;
        public Expression next;
        public Statement body;

        internal For(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Execute(Environment env)
        {
            Trace();
            HeronObject initVal = initial.Eval(env);
            env.AddVar(name, initVal);
            while (true)
            {
                HeronObject condVal = condition.Eval(env);
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
                + " = " + initial.ToString() 
                + "; " + condition.ToString() 
                + "; " + next.ToString() 
                + ")\n" + body.ToString();
        }

        public override string StatementType()
        {
            return "for_statement";
        }
    }

    public class CodeBlock : Statement
    {
        public List<Statement> statements = new List<Statement>();
        
        internal CodeBlock(Peg.AstNode node)
            : base(node)
        {
        }

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

        public override string StatementType()
        {
            return "code_block";
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{\n");
            foreach (Statement s in statements) {
                sb.Append(s);
                sb.Append("\n");
            }
            sb.Append("}\n");
            return sb.ToString();
        }
    }

    public class If : Statement
    {
        public Expression condition;
        public Statement ontrue;
        public Statement onfalse;

        internal If(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Execute(Environment env)
        {
            Trace();
            bool b = condition.Eval(env).ToBool();
            if (b)
                ontrue.Execute(env); 
            else
                if (onfalse != null)
                    onfalse.Execute(env);
        }

        public override string StatementType()
        {
            return "if_statement";
        }
    }

    public class While : Statement
    {
        public Expression cond;
        public Statement body;

        internal While(Peg.AstNode node)
            : base(node)
        {
        }

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

        public override string StatementType()
        {
            return "while_statement";
        }
    }

    public class Return : Statement
    {
        public Expression expression;

        internal Return(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Execute(Environment env)
        {
            Trace();
            env.Return(expression.Eval(env));
        }

        public override string StatementType()
        {
            return "return_statement";
        }
    }
}
