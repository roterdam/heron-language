using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    public abstract class Statement 
    {
        static List<Statement> noStatements = new List<Statement>();
        static List<Expression> noExpressions = new List<Expression>();
        static List<string> noStrings = new List<string>();

        public Peg.AstNode node;

        public abstract void Eval(HeronExecutor vm);

        internal Statement(Peg.AstNode node)
        {
            this.node = node;
        }

        public abstract string StatementType();
        
        public virtual IEnumerable<Statement> GetSubStatements()
        {
            return noStatements;
        }

        public virtual IEnumerable<Expression> GetSubExpressions()
        {
            return noExpressions;
        }

        public virtual IEnumerable<string> GetDefinedNames()
        {
            return noStrings;
        }

        public virtual IEnumerable<Statement> GetStatementTree()
        {
            yield return this;
            foreach (Statement x in GetSubStatements())
                foreach (Statement y in GetStatementTree())
                    yield return y;
        }

        public IEnumerable<Expression> GetExpressionTree()
        {
            foreach (Expression x in GetSubExpressions())
                foreach (Expression y in x.GetExpressionTree())
                    yield return y;
        }

        public IEnumerable<string> GetUsedNames()
        {
            foreach (Expression x in GetExpressionTree())
                if (x is Name)
                    yield return (x as Name).name;
        }

        public void GetUndefinedNames(Stack<string> names, List<string> result)
        {
            var newNames = new List<string>(GetDefinedNames());
            foreach (string name in newNames)
                names.Push(name);
            foreach (string name in GetUsedNames())
                if (!names.Contains(name) && !result.Contains(name))
                    result.Add(name);
            foreach (Statement st in GetSubStatements())
                st.GetUndefinedNames(names, result);
            for (int i=0; i < newNames.Count; ++i)
                names.Pop();
        }
    }

    public class VariableDeclaration : Statement
    {
        public string name;
        public string type;
        public Expression value;

        internal VariableDeclaration(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Eval(HeronExecutor vm)
        {
            HeronValue initVal = vm.Eval(value);
            vm.AddVar(name, initVal);
        }

        public override string ToString()
        {
            string r = "var " + name + " : " + type;
            if (value != null)
                r += " = " + value.ToString();
            return r;
        }

        public override string StatementType()
        {
            return "variable_declaration";
        }

        public override IEnumerable<Expression> GetSubExpressions()
        {
            if (value != null)
                yield return value;
        }

        public override IEnumerable<string> GetDefinedNames()
        {
            yield return name;
        }
    }

    public class DeleteStatement : Statement
    {
        public Expression expression;

        internal DeleteStatement(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Eval(HeronExecutor vm)
        {
            // TODO: check if the expression is a name.
            // if so, then set it to NULL. 
            // then dispose of it, etc. 
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

        public override IEnumerable<Expression> GetSubExpressions()
        {            
            yield return expression;
        }
    }

    public class ExpressionStatement : Statement
    {
        public Expression expression;

        internal ExpressionStatement(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Eval(HeronExecutor vm)
        {
            vm.Eval(expression);
        }

        public override string ToString()
        {
            return expression.ToString();
        }

        public override string StatementType()
        {
            return "expression_statement";
        }

        public override IEnumerable<Expression> GetSubExpressions()
        {
            yield return expression;
        }
    }

    public class ForEachStatement : Statement
    {
        public string name;
        public Expression collection;
        public Statement body;

        // TODO: make this a real Heron type
        public string type;

        internal ForEachStatement(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Eval(HeronExecutor vm)
        {
            // TODO: make this exception safe. Have a "using" with a special scope 
            // construction object.
            vm.PushScope();
            vm.AddVar(name, HeronValue.Null);
            HeronValue c = vm.Eval(this.collection);
            if (!(c is DotNetObject))
                throw new Exception("Unable to iterate over " + collection.ToString() + " because it is not a collection");

            DotNetObject tmp = c as DotNetObject;
            Object o = tmp.ToSystemObject();
            IEnumerable list = o as IEnumerable;
            if (list == null)
            {
                HeronCollection hc = o as HeronCollection;
                if (hc == null)
                    throw new Exception("Unable to iterate over " + collection.ToString() + " because it is not a collection");
                list = hc.InternalGetList();
                if (list == null)
                    throw new Exception("Unable to iterate over " + collection.ToString() + " because the internal collection was not set");
            }

            foreach (Object e in list) {
                HeronValue ho = DotNetObject.Marshal(e);
                vm.SetVar(name, ho);
                vm.Eval(body);
                if (vm.ShouldExitScope())
                    break;
            }
            vm.PopScope();
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

        public override IEnumerable<Statement> GetSubStatements()
        {
            yield return body;
        }

        public override IEnumerable<Expression> GetSubExpressions()
        {
            yield return collection;
        }

        public override IEnumerable<string> GetDefinedNames()
        {
            yield return name;
        }
    }

    public class ForStatement : Statement
    {
        public string name;
        public Expression initial;
        public Expression condition;
        public Expression next;
        public Statement body;

        internal ForStatement(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Eval(HeronExecutor vm)
        {
            HeronValue initVal = initial.Eval(vm);
            vm.AddVar(name, initVal);
            while (true)
            {
                HeronValue condVal = vm.Eval(condition);
                bool b = condVal.ToBool();
                if (!b)
                    break;
                vm.Eval(body);
                if (vm.ShouldExitScope())
                    break;
                vm.Eval(next);
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

        public override IEnumerable<Statement> GetSubStatements()
        {
            yield return body;
        }

        public override IEnumerable<Expression> GetSubExpressions()
        {
            yield return initial;
            yield return condition;
            yield return next;
        }

        public override IEnumerable<string> GetDefinedNames()
        {
            yield return name;
        }
    }

    public class CodeBlock : Statement
    {
        public List<Statement> statements = new List<Statement>();
        
        internal CodeBlock(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Eval(HeronExecutor vm)
        {
            vm.PushScope();
            foreach (Statement s in statements)
            {
                vm.Eval(s);
                if (vm.ShouldExitScope())
                    break;
            }
            vm.PopScope();
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

        public override IEnumerable<Statement> GetSubStatements()
        {
            return statements;
        }
    }

    public class IfStatement : Statement
    {
        public Expression condition;
        public Statement ontrue;
        public Statement onfalse;

        internal IfStatement(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Eval(HeronExecutor vm)
        {
            bool b = condition.Eval(vm).ToBool();
            if (b)
                vm.Eval(ontrue); 
            else
                if (onfalse != null)
                    vm.Eval(onfalse);
        }

        public override string StatementType()
        {
            return "if_statement";
        }

        public override IEnumerable<Statement> GetSubStatements()
        {
            yield return ontrue;
            if (onfalse != null)
                yield return onfalse;
        }

        public override IEnumerable<Expression> GetSubExpressions()
        {
            yield return condition;
        }
    }

    public class WhileStatement : Statement
    {
        public Expression condition;
        public Statement body;

        internal WhileStatement(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Eval(HeronExecutor vm)
        {
            while (true)
            {
                HeronValue o = condition.Eval(vm);
                bool b = o.ToBool();
                if (!b)
                    break;
                vm.Eval(body);
                if (vm.ShouldExitScope())
                    break;
            }
        }

        public override string StatementType()
        {
            return "while_statement";
        }

        public override IEnumerable<Statement> GetSubStatements()
        {
            yield return body;
        }

        public override IEnumerable<Expression> GetSubExpressions()
        {
            yield return condition;
        }
    }

    public class ReturnStatement : Statement
    {
        public Expression expression;

        internal ReturnStatement(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Eval(HeronExecutor vm)
        {
            HeronValue result = vm.Eval(expression);
            vm.Return(result);
        }

        public override string StatementType()
        {
            return "return_statement";
        }

        public override IEnumerable<Expression> GetSubExpressions()
        {
            yield return expression;
        }
    }

    public class SwitchStatement : Statement
    {
        public Expression condition;
        public List<CaseStatement> cases;
        public CodeBlock ondefault;
        
        internal SwitchStatement(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Eval(HeronExecutor vm)
        {
            HeronValue o = condition.Eval(vm);
            foreach (CaseStatement c in cases)
            {
                HeronValue cond = vm.Eval(c.condition);
                if (o.EqualsValue(cond))
                {
                    vm.Eval(c.statement);
                    return;
                }
            }
            if (ondefault != null)
            {
                vm.Eval(ondefault);
            }
        }

        public override string StatementType()
        {
            return "switch_statement";
        }

        public override IEnumerable<Statement> GetSubStatements()
        {
            yield return this;
            foreach (Statement st in cases)
                yield return st;
            if (ondefault != null)
                yield return ondefault;
        }

        public override IEnumerable<Expression> GetSubExpressions()
        {
            yield return condition;
        }
    }

    public class CaseStatement : Statement
    {
        public Expression condition;
        public CodeBlock statement;

        internal CaseStatement(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Eval(HeronExecutor vm)
        {
            vm.Eval(statement);
        }

        public override string StatementType()
        {
            return "switch_statement";
        }

        public override IEnumerable<Expression> GetSubExpressions()
        {
            yield return condition;
        }
    }
}
