/// Heron language interpreter for Windows in C#
/// http://www.heron-language.com
/// Copyright (c) 2009 Christopher Diggins
/// Licenced under the MIT License 1.0 
/// http://www.opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Diagnostics;

namespace HeronEngine
{
    public delegate HeronValue Evaluator(VM vm);

    /// <summary>
    /// An expression in its represtantation as part of the abstract syntax tree.     
    /// </summary>
    public abstract class Expression : HeronValue
    {
        protected void Error(string s)
        {
            throw new Exception("Error occured in expression " + GetType().Name + " : " + s);
        }

        protected void Assure(bool b, string s)
        {
            if (!b)
                Error(s);
        }

        public abstract HeronValue Eval(VM vm);

        public IEnumerable<Expression> GetSubExpressions()
        {
            foreach (FieldInfo fi in GetInstanceFields())
            {
                if (fi.FieldType.Equals(typeof(ExpressionList)))
                {
                    foreach (Expression expr in fi.GetValue(this) as ExpressionList)
                        yield return expr;
                }
                if (fi.FieldType.Equals(typeof(Expression)))
                {
                    yield return fi.GetValue(this) as Expression;
                }
            }
        }
        
        public IEnumerable<Expression> GetExpressionTree()
        {
            yield return this;
            foreach (Expression x in GetSubExpressions())
                if (x != null)
                    foreach (Expression y in x.GetExpressionTree())
                        if (y != null)
                            yield return y;
        }

        public void ResolveAllTypes(ModuleDefn m)
        {
            foreach (Expression x in GetExpressionTree())
                x.ResolveTypes(m);
        }

        public virtual void ResolveTypes(ModuleDefn m)
        {
            foreach (FieldInfo fi in GetInstanceFields())
            {
                if (fi.FieldType.Equals(typeof(HeronType)))
                {
                    HeronType t = fi.GetValue(this) as HeronType;
                    if (t == null)
                        throw new Exception("The type field cannot be null");
                    UnresolvedType ut = t as UnresolvedType;
                    if (ut != null)
                        fi.SetValue(this, ut.Resolve(m));
                }
            }
        }

        public abstract Expression Optimize(VM vm);
    }

    /// <summary>
    /// Used to construct optimized versions of expressions for 
    /// evaluation inside of tight loops.
    /// </summary>
    public class OptimizedExpression : Expression
    {
        Evaluator e;
        
        public OptimizedExpression(Evaluator e)
        {
            this.e = e;
        }

        public override HeronValue Eval(VM vm)
        {
            return e(vm);
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.OptimizedExpressionType;
        }

        public override Expression Optimize(VM vm)
        {
            return this;
        }
    }

    /// <summary>
    /// A list of expressions, used primarily for passing arguments to functions
    /// </summary>
    public class ExpressionList : List<Expression>
    {
        public override string ToString()
        {
            string s = "(";
            for (int i = 0; i < Count; ++i)
            {
                if (i > 0) s += ",";
                s += this[i].ToString();
            }
            s += ")";
            return s;
        }

        public HeronValue[] Eval(VM vm)
        {
            List<HeronValue> list = new List<HeronValue>();
            foreach (Expression x in this)
                list.Add(x.Eval(vm));
            return list.ToArray();
        }

        public ExpressionList Optimize(VM vm)
        {
            ExpressionList r = new ExpressionList();
            foreach (Expression x in this)
            {
                r.Add(x.Optimize(vm));
            }
            return r;
        }
    }

    /// <summary>
    /// Represents an assignment to ta variable or member variable.
    /// </summary>
    public class Assignment : Expression
    {
        [HeronVisible] public Expression lvalue;
        [HeronVisible] public Expression rvalue;

        public Assignment(Expression lvalue, Expression rvalue)
        {
            this.lvalue = lvalue;
            this.rvalue = rvalue;
        }

        public override Expression Optimize(VM vm)
        {
            Expression opt_rvalue = rvalue.Optimize(vm);

            if (lvalue is Name)
            {
                Name nm = lvalue as Name;
                VM.Accessor acc = vm.GetAccessor(nm.name);
                if (acc == null)
                    throw new Exception("Could not find name " + nm.name);
                return new OptimizedExpression((VM v) =>
                {
                    HeronValue x = opt_rvalue.Eval(v);
                    acc.Set(x);
                    return x;
                });
            }
            else
            {
                return new Assignment(lvalue.Optimize(vm), opt_rvalue);
            }
        }

        public override HeronValue Eval(VM vm)
        {            
            HeronValue val = vm.Eval(rvalue);

            if (lvalue is Name)
            {
                string name = (lvalue as Name).name;
                if (vm.HasVar(name))
                {
                    vm.SetVar(name, val);
                    return val;
                }
                else if (vm.HasField(name))
                {
                    vm.SetField(name, val);
                    return val;
                }
                else
                {
                    throw new Exception(name + " is not a member field or local variable that can be assigned to");
                }
            }
            else if (lvalue is ChooseField)
            {
                ChooseField field = lvalue as ChooseField;
                HeronValue self = vm.Eval(field.self);
                self.SetField(field.name, val);
                return val;
            }
            else if (lvalue is ReadAt)
            {
                // TODO: 
                // This is for "ta[x] = y"
                throw new Exception("Unimplemented");
            }
            else
            {
                throw new Exception("Cannot assign to expression " + lvalue.ToString());
            }
        }

        public override string ToString()
        {
            return lvalue.ToString() + " = " + rvalue.ToString();
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.Assignment;
        }
    }

    /// <summary>
    /// Represents access of ta member field (or method) of an object
    /// </summary>
    public class ChooseField : Expression
    {
        [HeronVisible] public string name;
        [HeronVisible] public Expression self;

        public ChooseField(Expression self, string name)
        {
            this.self = self;
            this.name = name;
        }

        public override HeronValue Eval(VM vm)
        {
            HeronValue x = self.Eval(vm);
            if (x == null)
                throw new Exception("Cannot select field '" + name + "' from a null object: " + self.ToString());
            HeronValue r = x.GetFieldOrMethod(name);
            if (r == null)
                throw new Exception("Could not resolve name " + name + " on expression " + self.ToString());
            return r;
        }

        public override Expression Optimize(VM vm)
        {
            return new ChooseField(self.Optimize(vm), name);
        }

        public override string ToString()
        {
            return "(" + self.ToString() + "." + name + ")";
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.ChooseField;
        }
    }

    /// <summary>
    /// Represents indexing of an object, like you would of an array or dictionary.
    /// </summary>
    public class ReadAt : Expression
    {
        [HeronVisible] public Expression self;
        [HeronVisible] public Expression index;

        public ReadAt(Expression coll, Expression index)
        {
            this.self = coll;
            this.index = index;
        }

        public override HeronValue Eval(VM vm)
        {
            HeronValue o = self.Eval(vm);
            HeronValue i = index.Eval(vm);
            return o.GetAtIndex(i);
        }

        public override string ToString()
        {
            return self + "[" + index.ToString() + "]";
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.ReadAt;
        }

        public override Expression Optimize(VM vm)
        {
            return new ReadAt(self.Optimize(vm), index.Optimize(vm));
        }
    }

    /// <summary>
    /// Represents an expression that instantiates ta class.
    /// </summary>
    public class NewExpr : Expression
    {
        [HeronVisible] public HeronType type;
        [HeronVisible] public ExpressionList args;
        [HeronVisible] public Expression modexpr;

        public NewExpr(HeronType type, ExpressionList args, Expression modexpr)
        {
            this.type = type;
            this.args = args;
            this.modexpr = modexpr;
        }

        public override HeronValue Eval(VM vm)
        {
            HeronValue[] argvals = args.Eval(vm);
            
            if (modexpr == null)
            {
                return type.Instantiate(vm, argvals, null);
            }
            else
            {
                HeronValue v = vm.Eval(modexpr);
                if (!(v is ModuleInstance))
                    throw new Exception("Expected a module, from " + modexpr.ToString() + " instead got value of type " + v.GetHeronType().ToString());
                ModuleInstance module = v as ModuleInstance;
                return type.Instantiate(vm, argvals, module);
            }
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.NewExpr;
        }

        public override Expression Optimize(VM vm)
        {
            return new NewExpr(type, args.Optimize(vm), modexpr.Optimize(vm));
        }
    }

    /// <summary>
    /// Represents the value returned by the keyword "null"
    /// </summary>
    public class NullExpr : Expression
    {
        public NullExpr()
        {
        }

        public override HeronValue Eval(VM vm)
        {
            return HeronValue.Null;
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.NullExpr;
        }

        public override Expression Optimize(VM vm)
        {
            return this;
        }
    }

    /// <summary>
    /// Represents literal constants.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Literal<T> : Expression where T : HeronValue
    {
        T val;

        public Literal(T x)
        {
            val = x;
        }

        public override HeronValue Eval(VM vm)
        {
            return val;
        }

        public override string ToString()
        {
            return val.ToString();
        }

        [HeronVisible]
        public HeronValue GetValue()
        {
            return val;
        }

        public override Expression Optimize(VM vm)
        {
            return this;
        }
    }

    /// <summary>
    /// Constant integer literal expression
    /// </summary>
    public class IntLiteral : Literal<IntValue>
    {
        public IntLiteral(int x)
            : base(new IntValue(x))
        {
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.IntLiteral;
        }
    }

    /// <summary>
    /// Constant boolean literal expression
    /// </summary>
    public class BoolLiteral : Literal<BoolValue>
    {
        public BoolLiteral(bool x)
            : base(new BoolValue(x))
        {
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.BoolLiteral;
        }
    }

    /// <summary>
    /// Constant floating point literal expression
    /// </summary>
    public class FloatLiteral : Literal<FloatValue>
    {
        public FloatLiteral(float x)
            : base(new FloatValue(x))
        {
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.FloatLiteral;
        }
    }

    /// <summary>
    /// Constant character literal expression
    /// </summary>
    public class CharLiteral : Literal<CharValue> 
    {
        public CharLiteral(char x)
            : base(new CharValue(x))
        {
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.CharLiteral;
        }

        public override string ToString()
        {
            return "'" + GetValue().ToString() + "'";
        }
    }

    /// <summary>
    /// Constant string literal expression
    /// </summary>
    public class StringLiteral : Literal<StringValue>
    {
        public StringLiteral(string x)
            : base(new StringValue(x))
        {
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.StringLiteral;
        }

        public override string ToString()
        {
            return "\"" + base.ToString() + "\"";
        }
    }

    /// <summary>
    /// An identifier expression. Could be ta function name, variable name, etc.
    /// </summary>
    public class Name : Expression
    {
        [HeronVisible] public string name;

        public Name(string s)
        {
            name = s;
        }

        public override HeronValue Eval(VM vm)
        {
            return vm.LookupName(name);
        }

        public override string ToString()
        {
            return name;
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.Name;
        }

        public override Expression Optimize(VM vm)
        {
            VM.Accessor acc = vm.GetAccessor(name);
            return new OptimizedExpression((VM v) => acc.Get()); 
        }
    }

    /// <summary>
    /// Represents ta function call expression.
    /// </summary>
    public class FunCall : Expression
    {
        [HeronVisible] public Expression funexpr;
        [HeronVisible] public ExpressionList args;

        public FunCall(Expression f, ExpressionList args)
        {
            funexpr = f;
            this.args = args;
        }

        public override Expression Optimize(VM vm)
        {
            return new FunCall(funexpr.Optimize(vm), args.Optimize(vm)); 
        }
        
        public override HeronValue Eval(VM vm)
        {
            HeronValue[] argvals = args.Eval(vm);
            HeronValue f = funexpr.Eval(vm);
            return f.Apply(vm, argvals);
        }

        public override string ToString()
        {
            return funexpr.ToString() + args.ToString();
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.FunCall;
        }
    }

    /// <summary>
    /// Represents an expression with ta unary operator. That is with one operand (e.g. the not operator '!' or the negation operator '-').
    /// This does not include the post-increment operator.
    /// </summary>
    public class UnaryOperation : Expression
    {
        [HeronVisible] public Expression operand;
        [HeronVisible] public string operation;

        public UnaryOperation(string sOp, Expression x)
        {
            operation = sOp;
            operand = x;
        }

        public override Expression Optimize(VM vm)
        {
            return new UnaryOperation(operation, operand.Optimize(vm))   ;
        }

        public override HeronValue Eval(VM vm)
        {
            HeronValue o = operand.Eval(vm);
            return o.InvokeUnaryOperator(vm, operation);
        }

        public override string ToString()
        {
            return "(" + operation + "  " + operand.ToString() + ")";
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.UnaryOperation;
        }
    }

    /// <summary>
    /// An anonymous function expression. An anonymous function may be ta closure, 
    /// if it has free variables. A free variable is ta variable that is not local
    /// to the function and that is not an argument.
    /// </summary>
    public class FunExpr : Expression
    {       
        [HeronVisible] public FormalArgs formals;
        [HeronVisible] public CodeBlock body;
        [HeronVisible] public HeronType rettype;
        [HeronVisible] public bool nullable;

        private FunctionDefn function;

        public override void ResolveTypes(ModuleDefn m)
        {
            formals.ResolveTypes(m);
            body.ResolveTypes(m);
            rettype = rettype.Resolve(m);
        }

        public override Expression Optimize(VM vm)
        {
            return this;
        }

        public override HeronValue Eval(VM vm)
        {
            FunctionValue fo = new FunctionValue(null, GetFunction());
            fo.ComputeFreeVars(vm);
            return fo;
        }

        public override string ToString()
        {
            return "function" + formals.ToString() + body.ToString();
        }

        private FunctionDefn GetFunction()
        {
            if (function == null)
            {
                function = new FunctionDefn(null);
                function.formals = formals;
                function.body = body;
                function.rettype = rettype;
            }
            return function;            
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.AnonFunExpr;
        }
    }

    /// <summary>
    /// An expression that is modified by the post-increment operator.
    /// It is converted to an assignment of the variable to itself plus one
    /// </summary>
    public class PostIncExpr : Expression
    {
        [HeronVisible] public Expression expr;
        
        public Assignment ass;

        public PostIncExpr(Expression x)
        {
            expr = x;
            ass = new Assignment(x, new BinaryOperation("+", x, new IntLiteral(1)));
        }

        public override HeronValue Eval(VM vm)
        {
            HeronValue result = vm.Eval(expr);
            vm.Eval(ass);
            return result;
        }

        public override Expression Optimize(VM vm)
        {
            return new PostIncExpr(expr.Optimize(vm));
        }

        public override string ToString()
        {
            return expr.ToString() + "++";
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.PostIncExpr;
        }
    }

    /// <summary>
    /// Represents an expression involving the "select" operator
    /// which filters ta list depending on ta predicate.
    /// </summary>
    public class SelectExpr : Expression
    {
        [HeronVisible] public string name;
        [HeronVisible] public Expression list;
        [HeronVisible] public Expression pred;

        public SelectExpr(string name, Expression list, Expression pred)
        {
            this.name = name;
            this.list = list;
            this.pred = pred;
        }

        public override Expression Optimize(VM vm)
        {
            return this;
        }

        public override HeronValue Eval(VM vm)
        {
            SeqValue seq = vm.EvalList(list);

            // TODO: finish, very important!
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return "select (" + name + " from " + list.ToString() + ") where " + pred.ToString();
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.SelectExpr;
        }
    }

    /// <summary>
    /// Represents an expression that involves the accumulate operator.
    /// This transforms ta list into ta single value by applying ta binary function
    /// to an accumulator and each item in the list consecutively.
    /// </summary>
    public class AccumulateExpr : Expression
    {
        [HeronVisible] public string acc;
        [HeronVisible] public Expression init;
        [HeronVisible] public string each;
        [HeronVisible] public Expression list;
        [HeronVisible] public Expression expr;

        public AccumulateExpr(string acc, Expression init, string each, Expression list, Expression expr)
        {
            this.acc = acc;
            this.each = each;
            this.init = init;
            this.list = list;
            this.expr = expr;
        }

        public override Expression Optimize(VM vm)
        {
            return this;
        }
        
        public override HeronValue Eval(VM vm)
        {
            using (vm.CreateScope())
            {
                vm.AddVar(acc, vm.Eval(init));
                vm.AddVar(each, HeronValue.Null);

                foreach (HeronValue x in vm.EvalListAsDotNet(list))
                {
                    vm.SetVar(each, x);
                    vm.SetVar(acc, vm.Eval(expr));
                }

                return vm.LookupName(acc);
            }
        }

        public override string ToString()
        {
            return "accumulate (" + acc + " = " + init.ToString() + " forall " + each + " in " + list.ToString() + ") " + expr.ToString();
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.AccumulateExpr;
        }
    }

    /// <summary>
    /// Represents ta literal list expression, such as [1, 'q', "hello"]
    /// </summary>
    public class TupleExpr : Expression
    {
        [HeronVisible] public ExpressionList exprs;

        public TupleExpr(ExpressionList xs)
        {
            exprs = xs;
        }

        public override Expression Optimize(VM vm)
        {
            return new TupleExpr(exprs.Optimize(vm));
        }

        public override HeronValue Eval(VM vm)
        {
            ListValue list = new ListValue();
            foreach (Expression expr in exprs)
                list.Add(vm.Eval(expr));
            return list;
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.TupleExpr;
        }
    }

    /// <summary>
    /// Represents ta literal table expression, such as 
    ///   table(ta:Int, s:String) { 1, "one"; 2, "two"; }
    /// </summary>
    public class TableExpr : Expression
    {
        [HeronVisible] public List<ExpressionList> rows = new List<ExpressionList>();
        [HeronVisible] public FormalArgs fielddefs; 

        public TableExpr()
        {
        }

        public override void ResolveTypes(ModuleDefn m)
        {
            foreach (ExpressionList row in rows)
                foreach (Expression field in row)
                    field.ResolveTypes(m);
            fielddefs.ResolveTypes(m);
        }

        private RecordLayout ComputeRecordLayout()
        {
            List<string> names = new List<string>();
            List<HeronType> types = new List<HeronType>();
            foreach (FormalArg arg in fielddefs)
            {
                names.Add(arg.name);
                types.Add(arg.type);
            }
            return new RecordLayout(names, types);
        }

        public void AddRow(ExpressionList row)
        {
            if (row.Count != fielddefs.Count)
                throw new Exception("The row has the incorrect number of fields, " + row.Count.ToString() + " expected " + fielddefs.Count.ToString());
            rows.Add(row);
        }

        public override Expression Optimize(VM vm)
        {
            return this;
        }

        public override HeronValue Eval(VM vm)
        {
            RecordLayout layout = ComputeRecordLayout();
            TableValue r = new TableValue(layout);
            foreach (ExpressionList row in rows)
            {
                List<HeronValue> vals = new List<HeronValue>();
                for (int i=0; i < row.Count; ++i)
                    vals.Add(vm.Eval(row[i]));
                RecordValue rv = new RecordValue(layout, vals);
                r.Add(rv);
            }
            return r;
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.TupleExpr;
        }
    }

    /// <summary>
    /// Represents ta literal record expression, such as 
    ///   record(ta:Int, s:String) { 1, "one" }
    /// </summary>
    public class RecordExpr : Expression
    {
        [HeronVisible] public ExpressionList fields;
        [HeronVisible] public FormalArgs fielddefs;

        public RecordExpr()
        {
        }

        public RecordExpr(ExpressionList fields, FormalArgs args)
        {
            this.fields = fields;
            this.fielddefs = args;
        }

        public override Expression Optimize(VM vm)
        {
            return new RecordExpr(fields.Optimize(vm), fielddefs);
        }

        public override void ResolveTypes(ModuleDefn m)
        {
            foreach (Expression field in fields)
                field.ResolveTypes(m);
            fielddefs.ResolveTypes(m);
        }

        private RecordLayout ComputeRecordLayout()
        {
            List<string> names = new List<string>();
            List<HeronType> types = new List<HeronType>();
            foreach (FormalArg arg in fielddefs)
            {
                names.Add(arg.name);
                types.Add(arg.type);
            }
            return new RecordLayout(names, types);
        }

        public override HeronValue Eval(VM vm)
        {
            RecordLayout layout = ComputeRecordLayout();
            List<HeronValue> vals = new List<HeronValue>();
            for (int i = 0; i < fields.Count; ++i)
                vals.Add(vm.Eval(fields[i]));
            return new RecordValue(layout, vals);            
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.TupleExpr;
        }
    }
}

