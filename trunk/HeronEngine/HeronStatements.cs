/// Heron language interpreter for Windows in C#
/// http://www.heron-language.com
/// Copyright (c) 2009 Christopher Diggins
/// Licenced under the MIT License 1.0 
/// http://www.opensource.org/licenses/mit-license.php

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

        public abstract void Eval(HeronVM vm);

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

        public virtual IEnumerable<string> GetLocallyDefinedNames()
        {
            return noStrings;
        }

        public IEnumerable<string> GetDefinedNames()
        {
            foreach (Statement st in GetStatementTree())
                foreach (string name in st.GetLocallyDefinedNames())
                    yield return name;
        }

        public virtual IEnumerable<Statement> GetStatementTree()
        {
            yield return this;
            foreach (Statement x in GetSubStatements())
                foreach (Statement y in x.GetStatementTree())
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
            foreach (Statement st in GetStatementTree())
                foreach (Expression x in st.GetExpressionTree())
                    if (x is Name)
                        yield return (x as Name).name;
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

        public override void Eval(HeronVM vm)
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

        public override IEnumerable<string> GetLocallyDefinedNames()
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

        public override void Eval(HeronVM vm)
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

        public override void Eval(HeronVM vm)
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

        public override void Eval(HeronVM vm)
        {
            using (vm.CreateScope())
            {
                vm.AddVar(name, HeronValue.Null);
                HeronValue val = vm.Eval(this.collection);
                IHeronEnumerable col = val as IHeronEnumerable;
                if (col == null)
                    throw new Exception("Unable to iterate over " + collection.ToString() + " because it is not a collection");
                IHeronEnumerator iter = col.GetEnumerator(vm);
                if (iter == null)
                    throw new Exception("Missing iterator");
                iter.Reset();
                while (iter.MoveNext(vm)) 
                {
                    HeronValue local = iter.GetValue(vm);
                    vm.SetVar(name, local);
                    vm.Eval(body);
                    if (vm.ShouldExitScope())
                        return;
                }
            }
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

        public override IEnumerable<string> GetLocallyDefinedNames()
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

        public override void Eval(HeronVM vm)
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

        public override IEnumerable<string> GetLocallyDefinedNames()
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

        public override void Eval(HeronVM vm)
        {
            using (vm.CreateScope())
            {
                foreach (Statement s in statements)
                {
                    vm.Eval(s);
                    if (vm.ShouldExitScope())
                        return;
                }
            }
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

        public override void Eval(HeronVM vm)
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

        public override void Eval(HeronVM vm)
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

        public override void Eval(HeronVM vm)
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

        public override void Eval(HeronVM vm)
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

        public override void Eval(HeronVM vm)
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
