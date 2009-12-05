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
    public abstract class Statement : HeronValue
    {
        static List<Statement> noStatements = new List<Statement>();
        static List<Expression> noExpressions = new List<Expression>();
        static List<string> noStrings = new List<string>();

        public Peg.AstNode node;

        public abstract void Eval(VM vm);

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
        [HeronVisible]
        public string name;
        [HeronVisible]
        public string type;
        [HeronVisible]
        public Expression value;

        internal VariableDeclaration(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Eval(VM vm)
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

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.VariableDeclarationStatement;
        }
    }

    public class DeleteStatement : Statement
    {
        [HeronVisible]
        public Expression expression;

        internal DeleteStatement(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Eval(VM vm)
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

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.DeleteStatement;
        }
    }

    public class ExpressionStatement : Statement
    {
        [HeronVisible]
        public Expression expression;

        internal ExpressionStatement(Peg.AstNode node)
            : base(node)
        {
        }

        internal ExpressionStatement(Expression expr)
            : base(null)
        {
            expression = expr;
        }

        public override void Eval(VM vm)
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

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.ExpressionStatement;
        }
    }

    public class ForEachStatement : Statement
    {
        [HeronVisible]
        public string name;
        [HeronVisible]
        public Expression collection;
        [HeronVisible]
        public Statement body;

        [HeronVisible]
        // TODO: make this a real Heron type
        public string type;

        internal ForEachStatement(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Eval(VM vm)
        {
            using (vm.CreateScope())
            {
                vm.AddVar(name, HeronValue.Null);
                foreach (HeronValue x in vm.EvalListAsDotNet(collection))
                {
                    vm.SetVar(name, x);
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

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.ForEachStatement;
        }
    }

    public class ForStatement : Statement
    {
        [HeronVisible]
        public string name;
        [HeronVisible]
        public Expression initial;
        [HeronVisible]
        public Expression condition;
        [HeronVisible]
        public Expression next;
        [HeronVisible]
        public Statement body;

        internal ForStatement(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Eval(VM vm)
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

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.ForStatement;
        }
    }

    public class CodeBlock : Statement
    {
        [HeronVisible]
        public List<Statement> statements = new List<Statement>();
        
        internal CodeBlock(Peg.AstNode node)
            : base(node)
        {
        }

        public CodeBlock()
            : base(null)
        {
        }

        public override void Eval(VM vm)
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

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.CodeBlock;
        }
    }

    public class IfStatement : Statement
    {
        [HeronVisible]
        public Expression condition;
        [HeronVisible]
        public Statement ontrue;
        [HeronVisible]
        public Statement onfalse;

        internal IfStatement(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Eval(VM vm)
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

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.IfStatement;
        }
    }

    public class WhileStatement : Statement
    {
        [HeronVisible]
        public Expression condition;
        [HeronVisible]
        public Statement body;

        internal WhileStatement(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Eval(VM vm)
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

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.WhileStatement;
        }
    }

    public class ReturnStatement : Statement
    {
        [HeronVisible]
        public Expression expression;

        internal ReturnStatement(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Eval(VM vm)
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

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.ReturnStatement;
        }
    }

    public class SwitchStatement : Statement
    {
        [HeronVisible]
        public Expression condition;
        [HeronVisible]
        public List<CaseStatement> cases;
        [HeronVisible]
        public CodeBlock ondefault;
        
        internal SwitchStatement(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Eval(VM vm)
        {
            HeronValue o = condition.Eval(vm);
            foreach (CaseStatement c in cases)
            {
                HeronValue cond = vm.Eval(c.condition);
                if (o.EqualsValue(vm, cond))
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

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.SwitchStatement;
        }
    }

    public class CaseStatement : Statement
    {
        [HeronVisible]
        public Expression condition;
        [HeronVisible]
        public CodeBlock statement;

        internal CaseStatement(Peg.AstNode node)
            : base(node)
        {
        }

        public override void Eval(VM vm)
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

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.CaseStatement;
        }
    }
}
