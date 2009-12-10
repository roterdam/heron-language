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
using System.Reflection;

namespace HeronEngine
{
    public abstract class Statement : HeronValue
    {
        public AstNode node;

        public abstract void Eval(VM vm);

        internal Statement(AstNode node)
        {
            this.node = node;
        }

        public abstract string StatementType();
        
        public IEnumerable<Statement> GetSubStatements()
        {
            foreach (FieldInfo fi in GetInstanceFields())
            {
                if (typeof(Statement).IsAssignableFrom(fi.FieldType))
                {
                    Object o = fi.GetValue(this);
                    if (o != null)
                        yield return o as Statement;
                }
                else if (fi.FieldType.Equals(typeof(List<Statement>)))
                {
                    List<Statement> sts = fi.GetValue(this) as List<Statement>;
                    foreach (Statement st in sts)
                        yield return st;
                }
            }
        }

        public IEnumerable<Expression> GetSubExpressions()
        {
            foreach (FieldInfo fi in GetInstanceFields())
            {
                if (typeof(Expression).IsAssignableFrom(fi.FieldType))
                {
                    Object o = fi.GetValue(this);
                    if (o != null) 
                        yield return o as Expression;
                }
                else if (typeof(List<Expression>).IsAssignableFrom(fi.FieldType))
                {
                    List<Expression> xs = fi.GetValue(this) as List<Expression>;
                    foreach (Expression x in xs)
                        yield return x;
                }
            }
        }

        public virtual IEnumerable<string> GetLocallyDefinedNames()
        {
            yield break;
        }

        public IEnumerable<string> GetDefinedNames()
        {
            foreach (Statement st in GetStatementTree())
                foreach (string name in st.GetLocallyDefinedNames())
                    yield return name;
        }

        public IEnumerable<Statement> GetStatementTree()
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

        public void ResolveTypes()
        {
            foreach (FieldInfo fi in GetInstanceFields())
            {
                if (fi.FieldType.Equals(typeof(HeronType)))
                {
                    HeronType t = fi.GetValue(this) as HeronType;
                    if (t == null)
                        throw new Exception("The type field cannot be null, expected an UnresolvedType");
                    UnresolvedType ut = t as UnresolvedType;
                    if (ut != null)
                        fi.SetValue(this, ut.Resolve());
                }
            }

            foreach (Expression x in GetSubExpressions())
                x.ResolveAllTypes();
        }
    }

    public class VariableDeclaration : Statement
    {
        [HeronVisible] public string name;
        [HeronVisible] public HeronType type;
        [HeronVisible] public Expression value;

        internal VariableDeclaration(AstNode node)
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

        public override IEnumerable<string> GetLocallyDefinedNames()
        {
            yield return name;
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.VariableDeclaration;
        }
    }

    public class DeleteStatement : Statement
    {
        [HeronVisible] public Expression expression;

        internal DeleteStatement(AstNode node)
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

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.DeleteStatement;
        }
    }

    public class ExpressionStatement : Statement
    {
        [HeronVisible] public Expression expression;

        internal ExpressionStatement(AstNode node)
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

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.ExpressionStatement;
        }
    }

    public class ForEachStatement : Statement
    {
        [HeronVisible] public string name;
        [HeronVisible] public Expression collection;
        [HeronVisible] public Statement body;
        [HeronVisible] public HeronType type;

        internal ForEachStatement(AstNode node)
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
        [HeronVisible] public string name;
        [HeronVisible] public Expression initial;
        [HeronVisible] public Expression condition;
        [HeronVisible] public Expression next;
        [HeronVisible] public Statement body;

        internal ForStatement(AstNode node)
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
        [HeronVisible] public List<Statement> statements = new List<Statement>();
        
        internal CodeBlock(AstNode node)
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

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.CodeBlock;
        }
    }

    public class IfStatement : Statement
    {
        [HeronVisible] public Expression condition;
        [HeronVisible] public Statement ontrue;
        [HeronVisible] public Statement onfalse;

        internal IfStatement(AstNode node)
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

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.IfStatement;
        }
    }

    public class WhileStatement : Statement
    {
        [HeronVisible] public Expression condition;
        [HeronVisible] public Statement body;

        internal WhileStatement(AstNode node)
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

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.WhileStatement;
        }
    }

    public class ReturnStatement : Statement
    {
        [HeronVisible] public Expression expression;

        internal ReturnStatement(AstNode node)
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

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.ReturnStatement;
        }
    }

    public class SwitchStatement : Statement
    {
        [HeronVisible] public Expression condition;
        [HeronVisible] public List<Statement> cases;
        [HeronVisible] public Statement ondefault;
        
        internal SwitchStatement(AstNode node)
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

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.SwitchStatement;
        }
    }

    public class CaseStatement : Statement
    {
        [HeronVisible] public Expression condition;
        [HeronVisible] public Statement statement;

        internal CaseStatement(AstNode node)
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

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.CaseStatement;
        }
    }
}
