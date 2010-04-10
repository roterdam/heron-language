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
        public ParseNode node;

        public abstract void Eval(VM vm);

        internal Statement(ParseNode node)
        {
            this.node = node;
        }

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

        public void ResolveTypes(ModuleDefn global, ModuleDefn m)
        {
            foreach (FieldInfo fi in GetInstanceFields())
            {
                if (fi.FieldType.Equals(typeof(HeronType)))
                {
                    HeronType t = fi.GetValue(this) as HeronType;
                    fi.SetValue(this, t.Resolve(global, m));
                }
                else if (fi.FieldType.Equals(typeof(VarDesc)))
                {
                    VarDesc vd = fi.GetValue(this) as VarDesc;
                    vd.ResolveTypes(global, m);
                }
            }

            foreach (Expression x in GetSubExpressions())
                x.ResolveAllTypes(global, m);
        }
    }

    public class VariableDeclaration : Statement
    {
        [HeronVisible] public VarDesc vardesc;
        [HeronVisible] public Expression value;

        internal VariableDeclaration(ParseNode node)
            : base(node)
        {
        }

        public override void Eval(VM vm)
        {
            if (value != null)
            {
                HeronValue v = vm.Eval(value);
                vm.AddVar(vardesc, v);
            }
            else
            {
                vm.AddVar(vardesc);
            }
        }

        public override string ToString()
        {
            string r = "var " + vardesc.name + " : " + vardesc.type;
            if (value != null)
                r += " = " + value.ToString();
            return r;
        }

        public override IEnumerable<string> GetLocallyDefinedNames()
        {
            yield return vardesc.name;
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.VariableDeclaration;
        }
    }

    public class DeleteStatement : Statement
    {
        [HeronVisible] public Expression expression;

        internal DeleteStatement(ParseNode node)
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

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.DeleteStatement;
        }
    }

    public class ExpressionStatement : Statement
    {
        [HeronVisible] public Expression expression;

        internal ExpressionStatement(ParseNode node)
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
        [HeronVisible] public bool nullable;

        internal ForEachStatement(ParseNode node)
            : base(node)
        {
        }

        public override void Eval(VM vm)
        {
            VarDesc desc = new VarDesc(name);
            foreach (HeronValue x in vm.EvalListAsDotNet(collection))
            {
                using (vm.CreateScope())
                {
                    vm.AddVar(desc, x);
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

        internal ForStatement(ParseNode node)
            : base(node)
        {
        }

        public override void Eval(VM vm)
        {
            HeronValue initVal = initial.Eval(vm);
            VarDesc desc = new VarDesc(name);
            using (vm.CreateScope())
            {
                vm.AddVar(desc, initVal);
                while (true)
                {
                    HeronValue condVal = vm.Eval(condition);
                    bool b = condVal.ToBool();
                    if (!b)
                        return;
                    vm.Eval(body);
                    if (vm.ShouldExitScope())
                        return;
                    vm.Eval(next);
                }
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
        
        internal CodeBlock(ParseNode node)
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

        internal IfStatement(ParseNode node)
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

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.IfStatement;
        }
    }

    public class WhileStatement : Statement
    {
        [HeronVisible] public Expression condition;
        [HeronVisible] public Statement body;

        internal WhileStatement(ParseNode node)
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

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.WhileStatement;
        }
    }

    public class ReturnStatement : Statement
    {
        [HeronVisible] public Expression expression;

        internal ReturnStatement(ParseNode node)
            : base(node)
        {
        }

        public override void Eval(VM vm)
        {
            if (expression != null)
            {
                HeronValue result = vm.Eval(expression);
                vm.Return(result);
            }
            else
            {
                vm.Return(HeronValue.Void);
            }
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
        
        internal SwitchStatement(ParseNode node)
            : base(node)
        {
        }

        public override void Eval(VM vm)
        {
            HeronValue o = condition.Eval(vm);
            foreach (CaseStatement c in cases)
            {
                HeronValue cond = vm.Eval(c.condition);
                if (o.Equals(cond))
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

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.SwitchStatement;
        }
    }

    public class CaseStatement : Statement
    {
        [HeronVisible] public Expression condition;
        [HeronVisible] public Statement statement;

        internal CaseStatement(ParseNode node)
            : base(node)
        {
        }

        public override void Eval(VM vm)
        {
            vm.Eval(statement);
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.CaseStatement;
        }
    }
}
