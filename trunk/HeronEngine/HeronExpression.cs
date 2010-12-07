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
    /// Speeds up access to variables.
    /// </summary>
    public class Accessor
    {
        HeronValue value;

        public Accessor(HeronValue val)
        {
            value = val;
        }

        public HeronValue Get()
        {
            return value;
        }

        public void Set(HeronValue value)
        {
            this.value = value;
        }
    }

    public class OptimizationParams
    {
        Dictionary<String, Accessor> accessors = new Dictionary<string, Accessor>();
        public Accessor AddNewAccessor(string name, HeronValue val)
        {
            Accessor acc = new Accessor(val);
            accessors.Add(name, acc);
            return acc;
        }
        public Accessor AddNewAccessor(string name)
        {
            return AddNewAccessor(name, HeronValue.Null);
        }
        public Accessor GetAccessor(string name)
        {
            if (!accessors.ContainsKey(name))
                return null;
            return accessors[name];
        }
    }

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

        public void ResolveAllTypes(ModuleDefn global, ModuleDefn m)
        {
            foreach (Expression x in GetExpressionTree())
                x.ResolveTypes(global, m);
        }

        public virtual void ResolveTypes(ModuleDefn global, ModuleDefn m)
        {
            foreach (FieldInfo fi in GetInstanceFields())
            {
                if (fi.FieldType.Equals(typeof(TypeRef)))
                {
                    TypeRef tr = fi.GetValue(this) as TypeRef;
                    if (tr == null)
                        throw new Exception("The type-reference field cannot be null");
                    tr.Resolve(global, m);
                }
            }
        }

        public abstract Expression Optimize(OptimizationParams op);

        public override HeronType Type
        {
            get { return PrimitiveTypes.Expression; }
        }
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

        public override HeronType Type
        {
            get { return PrimitiveTypes.OptimizedExpressionType; }
        }

        public override Expression Optimize(OptimizationParams op)
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

        public ExpressionList Optimize(OptimizationParams op)
        {
            ExpressionList r = new ExpressionList();
            foreach (Expression x in this)
            {
                r.Add(x.Optimize(op));
            }
            return r;
        }
    }

    /// <summary>
    /// Represents an assignment to a variable or member variable.
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

        public override Expression Optimize(OptimizationParams op)
        {
            Expression opt_rvalue = rvalue.Optimize(op);

            if (lvalue is Name)
            {
                Name nm = lvalue as Name;
                Accessor acc = op.GetAccessor(nm.name);
                if (acc == null)
                {
                    return this;
                }
                else
                {
                    return new OptimizedExpression((VM vm) =>
                    {
                        HeronValue x = opt_rvalue.Eval(vm);
                        acc.Set(x);
                        return x;
                    });
                }
            }
            else
            {
                return new Assignment(lvalue.Optimize(op), opt_rvalue);
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
                ReadAt ra = lvalue as ReadAt;
                HeronValue self = vm.Eval(ra.self);
                HeronValue index = vm.Eval(ra.index);
                ListValue list = self as ListValue;
                if (list != null)
                {
                    list.SetAtIndex(index, val);
                    return val;
                }
                ArrayValue array = self as ArrayValue;
                if (array != null)
                {
                    array.SetAtIndex(index, val);
                    return val;
                }

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

        public override HeronType Type
        {
           get { return PrimitiveTypes.Assignment; }
        }
    }

    /// <summary>
    /// Represents access of a member field (or method) of an object
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
                throw new Exception("Could not resolve name " + name + " on expression '" + self.ToString() + "'");
            return r;
        }

        public override Expression Optimize(OptimizationParams op)
        {
            return new ChooseField(self.Optimize(op), name);
        }

        public override string ToString()
        {
            return "(" + self.ToString() + "." + name + ")";
        }

        public override HeronType Type
        {
            get { return PrimitiveTypes.ChooseField; }
        }
    }

    /// <summary>
    /// Represents indexing of an object, like you would of an source or dictionary.
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

        public override HeronType Type
        {
            get { return PrimitiveTypes.ReadAt; }
        }

        public override Expression Optimize(OptimizationParams op)
        {
            return new ReadAt(self.Optimize(op), index.Optimize(op));
        }
    }

    /// <summary>
    /// Represents an expression that instantiates a class.
    /// </summary>
    public class NewExpr : Expression
    {
        [HeronVisible] public TypeRef type;
        [HeronVisible] public ExpressionList args;
        [HeronVisible] public string module;

        public NewExpr(TypeRef type, ExpressionList args, string module)
        {
            this.type = type;
            this.args = args;
            this.module = module;
        }

        public override HeronValue Eval(VM vm)
        {
            HeronValue[] argvals = args.Eval(vm);
            
            if (module == null || module.Length == 0)
            {
                return type.type.Instantiate(vm, argvals, vm.CurrentModuleInstance);
            }
            else
            {
                ModuleInstance mi = vm.FindModule(module);
                if (module == null)
                    throw new Exception("Could not find module " + module);
                return type.type.Instantiate(vm, argvals, mi);
            }
        }

        public override HeronType Type
        {
            get { return PrimitiveTypes.NewExpr; }
        }

        public override Expression Optimize(OptimizationParams op)
        {
            return new NewExpr(type, args.Optimize(op), module);
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

        public override HeronType Type
        {
            get { return PrimitiveTypes.NullExpr; }
        }

        public override Expression Optimize(OptimizationParams op)
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

        public override Expression Optimize(OptimizationParams op)
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

        public override HeronType Type
        {
            get { return PrimitiveTypes.IntLiteral; }
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

        public override HeronType Type
        {
            get { return PrimitiveTypes.BoolLiteral; }
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

        public override HeronType Type
        {
            get { return PrimitiveTypes.FloatLiteral; }
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

        public override HeronType Type
        {
            get { return PrimitiveTypes.CharLiteral; }
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

        public override HeronType Type
        {
            get { return PrimitiveTypes.StringLiteral; }
        }

        public override string ToString()
        {
            return "\"" + base.ToString() + "\"";
        }
    }

    /// <summary>
    /// An identifier expression. Could be a function name, variable name, etc.
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

        public override HeronType Type
        {
            get { return PrimitiveTypes.Name; }
        }

        public override Expression Optimize(OptimizationParams op)
        {
            Accessor acc = op.GetAccessor(name);
            if (acc == null)
                return this;
            return new OptimizedExpression((VM vm) => acc.Get()); 
        }
    }

    /// <summary>
    /// Represents a function call expression.
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

        public override Expression Optimize(OptimizationParams op)
        {
            return new FunCall(funexpr.Optimize(op), args.Optimize(op)); 
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

        public override HeronType Type
        {
            get { return PrimitiveTypes.FunCall; }
        }
    }

    /// <summary>
    /// Represents an expression with a unary operator. That is with one operand (e.g. the not operator '!' or the negation operator '-').
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

        public override Expression Optimize(OptimizationParams op)
        {
            return new UnaryOperation(operation, operand.Optimize(op))   ;
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

        public override HeronType Type
        {
            get { return PrimitiveTypes.UnaryOperation; }
        }
    }

    /// <summary>
    /// An anonymous function expression. An anonymous function may be a closure, 
    /// if it has free variables. A free variable is a variable that is not local
    /// to the function and that is not an argument.
    /// </summary>
    public class FunExpr : Expression
    {       
        [HeronVisible] public FormalArgs formals;
        [HeronVisible] public CodeBlock body;
        [HeronVisible] public TypeRef rettype;

        private FunctionDefn function;

        public override void ResolveTypes(ModuleDefn global, ModuleDefn m)
        {
            formals.ResolveTypes(global, m);
            body.ResolveTypes(global, m);
            rettype.Resolve(global, m);
        }

        public override Expression Optimize(OptimizationParams op)
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

        public override HeronType Type
        {
            get { return PrimitiveTypes.FunExpr; }
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

        public override Expression Optimize(OptimizationParams op)
        {
            return new PostIncExpr(expr.Optimize(op));
        }

        public override string ToString()
        {
            return expr.ToString() + "++";
        }

        public override HeronType Type
        {
            get { return PrimitiveTypes.PostIncExpr; }
        }
    }

    /// <summary>
    /// Represents an expression that involves the accumulate operator.
    /// This transforms a list into a single value by applying a binary function
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

        public override Expression Optimize(OptimizationParams op)
        {
            return this;
        }
        
        public override HeronValue Eval(VM vm)
        {
            using (vm.CreateScope())
            {
                vm.AddVar(new VarDesc(acc), vm.Eval(init));
                vm.AddVar(new VarDesc(each), HeronValue.Null);

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

        public override HeronType Type
        {
            get { return PrimitiveTypes.AccumulateExpr; }
        }
    }

    /// <summary>
    /// Represents a literal list expression, such as [1, 'q', "hello"]
    /// </summary>
    public class TupleExpr : Expression
    {
        [HeronVisible] public ExpressionList exprs;

        public TupleExpr(ExpressionList xs)
        {
            exprs = xs;
        }

        public override Expression Optimize(OptimizationParams op)
        {
            return new TupleExpr(exprs.Optimize(op));
        }

        public override HeronValue Eval(VM vm)
        {
            ListValue list = new ListValue(PrimitiveTypes.AnyType);
            foreach (Expression expr in exprs)
                list.Add(vm.Eval(expr));
            return list;
        }

        public override HeronType Type
        {
            get { return PrimitiveTypes.TupleExpr; }
        }
    }

    /// <summary>
    /// Represents a literal table expression, such as 
    ///   table(a:Int, s:String) { 1, "one"; 2, "two"; }
    /// </summary>
    public class TableExpr : Expression
    {
        [HeronVisible] public List<ExpressionList> rows = new List<ExpressionList>();
        [HeronVisible] public FormalArgs fielddefs; 

        public TableExpr()
        {
        }

        public override void ResolveTypes(ModuleDefn global, ModuleDefn m)
        {
            foreach (ExpressionList row in rows)
                foreach (Expression field in row)
                    field.ResolveTypes(global, m);
            fielddefs.ResolveTypes(global, m);
        }

        private RecordLayout ComputeRecordLayout()
        {
            List<string> names = new List<string>();
            List<HeronType> types = new List<HeronType>();
            foreach (FormalArg arg in fielddefs)
            {
                names.Add(arg.type.name);
                types.Add(arg.type.type);
            }
            return new RecordLayout(names, types);
        }

        public void AddRow(ExpressionList row)
        {
            if (row.Count != fielddefs.Count)
                throw new Exception("The row has the incorrect number of fields, " + row.Count.ToString() + " expected " + fielddefs.Count.ToString());
            rows.Add(row);
        }

        public override Expression Optimize(OptimizationParams op)
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

        public override HeronType Type
        {
            get { return PrimitiveTypes.TableExpr; }
        }
    }

    /// <summary>
    /// Represents a literal record expression, such as 
    ///   record(a:Int, s:String) { 1, "one" }
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

        public override Expression Optimize(OptimizationParams op)
        {
            return new RecordExpr(fields.Optimize(op), fielddefs);
        }

        public override void ResolveTypes(ModuleDefn global, ModuleDefn m)
        {
            foreach (Expression field in fields)
                field.ResolveTypes(global, m);
            fielddefs.ResolveTypes(global, m);
        }

        private RecordLayout ComputeRecordLayout()
        {
            List<string> names = new List<string>();
            List<HeronType> types = new List<HeronType>();
            foreach (FormalArg arg in fielddefs)
            {
                names.Add(arg.type.name);
                types.Add(arg.type.type);
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

        public override HeronType Type
        {
            get { return PrimitiveTypes.RecordExpr; }
        }
    }

    public class ParanthesizedExpr : Expression
    {
        [HeronVisible]
        public Expression expr;

        public ParanthesizedExpr(Expression expr)
        {
            this.expr = expr;
        }

        public override HeronValue Eval(VM vm)
        {
            return expr.Eval(vm);
        }

        public override Expression Optimize(OptimizationParams op)
        {
            return new ParanthesizedExpr(expr.Optimize(op));
        }

        public override HeronType Type
        {
            get { return PrimitiveTypes.ParanthesizedExpr; }
        }

        public override string ToString()
        {
            return "(" + expr.ToString() + ")";
        }
    }
}

