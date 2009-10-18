using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace HeronEngine
{
    /// <summary>
    /// An expression in its represtantation as part of the abstract syntax tree.     
    /// </summary>
    public abstract class Expression 
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

        public abstract HeronValue Eval(HeronExecutor vm);

        static protected List<Expression> noExpressions = new List<Expression>();

        public abstract IEnumerable<Expression> GetSubExpressions();

        public IEnumerable<Expression> GetExpressionTree()
        {
            yield return this;
            foreach (Expression x in GetSubExpressions())
                foreach (Expression y in x.GetExpressionTree())
                    yield return y;
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

        public HeronValue[] Eval(HeronExecutor vm)
        {
            List<HeronValue> list = new List<HeronValue>();
            foreach (Expression x in this)
                list.Add(x.Eval(vm));
            return list.ToArray();
        }
    }

    public class Assignment : Expression
    {
        public Expression lvalue;
        public Expression rvalue;

        public Assignment(Expression lvalue, Expression rvalue)
        {
            this.lvalue = lvalue;
            this.rvalue = rvalue;
        }

        public override HeronValue Eval(HeronExecutor vm)
        {            
            HeronValue val = rvalue.Eval(vm);

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
            else if (lvalue is SelectField)
            {
                SelectField sf = lvalue as SelectField;
                HeronValue self = sf.self.Eval(vm);
                string name = sf.name;
                self.SetField(name, val);
                return val;
            }
            else if (lvalue is ReadAt)
            {
                // TODO: 
                // This is for "a[x] = y"
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

        public override IEnumerable<Expression> GetSubExpressions()
        {
            yield return lvalue;
            yield return rvalue;
        }
    }

    public class SelectField : Expression
    {
        public string name;
        public Expression self;

        public SelectField(Expression self, string name)
        {
            this.self = self;
            this.name = name;
        }

        public override HeronValue Eval(HeronExecutor vm)
        {
            HeronValue x = self.Eval(vm);
            if (x == null)
                throw new Exception("Cannot select field '" + name + "' from a null object: " + self.ToString());
            return x.GetFieldOrMethod(name);
        }

        public override string ToString()
        {
            return "(" + self.ToString() + "." + name + ")";
        }

        public override IEnumerable<Expression> GetSubExpressions()
        {
            yield return self;
        }
    }

    public class ReadAt : Expression
    {
        public Expression coll;
        public Expression index;

        public ReadAt(Expression coll, Expression index)
        {
            this.coll = coll;
            this.index = index;
        }

        public override HeronValue Eval(HeronExecutor vm)
        {
            HeronValue o = coll.Eval(vm);
            HeronValue i = index.Eval(vm);
            return o.GetAtIndex(i);
        }

        public override string ToString()
        {
            return coll + "[" + index.ToString() + "]";
        }

        public override IEnumerable<Expression> GetSubExpressions()
        {
            yield return coll;
            yield return index;
        }
    }

    public class New : Expression
    {
        string type;
        ExpressionList args;

        public New(string type, ExpressionList args)
        {
            this.type = type;
            this.args = args;
        }

        public override HeronValue Eval(HeronExecutor vm)
        {
            HeronValue o = vm.LookupName(type);
            if (!(o is HeronType))
                throw new Exception("Cannot instantiate non-type " + type);
            HeronType t = o as HeronType;
            HeronValue[] argvals = args.Eval(vm);
            return t.Instantiate(vm, argvals);
        }

        public override IEnumerable<Expression> GetSubExpressions()
        {
            return args;
        }
    }

    public abstract class Literal<T> : Expression where T : HeronValue
    {
        T val;

        public Literal(T x)
        {
            val = x;
        }

        public override HeronValue Eval(HeronExecutor vm)
        {
            return val;
        }

        public override string ToString()
        {
            return val.ToString();
        }

        public override IEnumerable<Expression> GetSubExpressions()
        {
            return noExpressions;
        }
    }

    public class IntLiteral : Literal<IntValue>
    {
        public IntLiteral(int x)
            : base(new IntValue(x))
        {
        }
    }

    public class FloatLiteral : Literal<FloatValue>
    {
        public FloatLiteral(float x)
            : base(new FloatValue(x))
        {
        }
    }

    public class CharLiteral : Literal<CharValue> 
    {
        public CharLiteral(char x)
            : base(new CharValue(x))
        {
        }
    }

    public class StringLiteral : Literal<StringValue>
    {
        public StringLiteral(string x)
            : base(new StringValue(x))
        {
        }
    }

    public class Name : Expression
    {
        public string name;

        public Name(string s)
        {
            name = s;
        }

        public override HeronValue Eval(HeronExecutor vm)
        {
            HeronValue r = vm.LookupName(name);
            return r;
        }

        public override string ToString()
        {
            return name;
        }

        public override IEnumerable<Expression> GetSubExpressions()
        {
            return noExpressions;
        }
    }

    public class FunCall : Expression
    {
        public Expression funexpr;
        public ExpressionList args;

        public FunCall(Expression f, ExpressionList args)
        {
            funexpr = f;
            this.args = args;
        }
        
        public override HeronValue Eval(HeronExecutor vm)
        {
            HeronValue[] argvals = args.Eval(vm);
            HeronValue f = funexpr.Eval(vm);
            return f.Apply(vm, argvals);
        }

        public override string ToString()
        {
            return funexpr.ToString() + args.ToString();
        }

        public override IEnumerable<Expression> GetSubExpressions()
        {
            yield return funexpr;
            foreach (Expression x in args)
                yield return x;
        }
    }

    public class UnaryOperator : Expression
    {
        public Expression operand;
        public string operation;

        public UnaryOperator(string sOp, Expression x)
        {
            operation = sOp;
            operand = x;
        }

        public override HeronValue Eval(HeronExecutor vm)
        {
            HeronValue o = operand.Eval(vm);
            return o.InvokeUnaryOperator(operation);
        }

        public override string ToString()
        {
            return "(" + operation + "  " + operand.ToString() + ")";
        }

        public override IEnumerable<Expression> GetSubExpressions()
        {
            yield return operand;
        }
    }

    public class BinaryOperator : Expression
    {
        public Expression operand1;
        public Expression operand2;
        public string operation;

        public BinaryOperator(string sOp, Expression x, Expression y)
        {
            operation = sOp;
            operand1 = x;
            operand2 = y;
        }

        // TODO: improve efficiency. This is a pretty terrible operation
        public override HeronValue Eval(HeronExecutor vm)
        {
            HeronValue a = operand1.Eval(vm);
            HeronValue b = operand2.Eval(vm);

            if (a == null)
                throw new Exception("Left hand operand '" + operand1.ToString() + "' could not be evaluated");
            if (b == null)
                throw new Exception("Right hand operand '" + operand2.ToString() + "' could not be evaluated");

            if (operation == "is")
            {
                if (!(b is HeronType))
                    throw new Exception("The 'is' operator expects a type as a right hand argument");

                Any any;
                if (a is Any)
                    any = a as Any; 
                else
                    any = new Any(a);

                return new BoolValue(any.Is(b as HeronType));                    
            }
            else if (operation == "as")
            {
                if (!(b is HeronType))
                    throw new Exception("The 'as' operator expects a type as a right hand argument");

                Any any;
                if (a is Any)
                    any = a as Any;
                else
                    any = new Any(a);

                return any.As(b as HeronType);
            }
            else if (a is NullValue)
            {
                return a.InvokeBinaryOperator(operation, b);
            }
            else if (b is NullValue)
            {
                return b.InvokeBinaryOperator(operation, a);
            }
            else if (a is IntValue)
            {
                if (b is IntValue)
                {
                    return a.InvokeBinaryOperator(operation, b as IntValue);
                }
                else if (b is FloatValue)
                {
                    return (new FloatValue((a as IntValue).GetValue())).InvokeBinaryOperator(operation, b as FloatValue);
                }
                else
                {
                    throw new Exception("Incompatible types for binary operator " + a.GetType() + " and " + b.GetType());
                }
            }
            else if (a is FloatValue)
            {
                if (b is IntValue)
                {
                    return a.InvokeBinaryOperator(operation, new FloatValue((b as IntValue).GetValue()));
                }
                else if (b is FloatValue)
                {
                    return a.InvokeBinaryOperator(operation, b as FloatValue);
                }
                else
                {
                    throw new Exception("Incompatible types for binary operator " + operation + " : " + a.GetType() + " and " + b.GetType());
                }
            }
            else if (a is CharValue)
            {
                if (!(b is CharValue))
                    throw new Exception("Incompatible types for binary operator " + operation + " : " + a.GetType() + " and " + b.GetType());
                return a.InvokeBinaryOperator(operation, b as CharValue);
            }
            else if (a is StringValue)
            {
                if (!(b is StringValue))
                    throw new Exception("Incompatible types for binary operator " + operation + " : " + a.GetType() + " and " + b.GetType());
                return a.InvokeBinaryOperator(operation, b as StringValue);
            }
            else if (a is BoolValue)
            {
                if (!(b is BoolValue))
                    throw new Exception("Incompatible types for binary operator " + operation + " : " + a.GetType() + " and " + b.GetType());
                return a.InvokeBinaryOperator(operation, b as BoolValue);
            }
            else if (a is EnumInstance)
            {
                if (operation == "==" || operation == "!=")
                {
                    if (!(b is EnumInstance))
                        throw new Exception("Only an enumeration instance can be compared against an enumeration instance");
                    EnumInstance ea = a as EnumInstance;
                    EnumInstance eb = b as EnumInstance;

                    if (operation == "==")
                        return new BoolValue(ea.Equals(eb));
                    else
                        return new BoolValue(!ea.Equals(eb));
                }
                else
                {
                    throw new Exception("Operation '" + operation + "' is not supported on enumerations");
                }
            }
            else if (a is ClassInstance)
            {
                if (operation == "==" || operation == "!=")
                {
                    if (!(b is ClassInstance))
                        throw new Exception("Only a class instance can be compared against a class instance");
                    ClassInstance ca = a as ClassInstance;
                    ClassInstance cb = b as ClassInstance;

                    if (operation == "==")
                        return new BoolValue(ca.Equals(cb));
                    else
                        return new BoolValue(!ca.Equals(cb));
                }
                else
                {
                    throw new Exception("Operation '" + operation + "' is not supported on class instances");
                }
            }
            else if (a is InterfaceInstance)
            {
                if (operation == "==" || operation == "!=")
                {
                    if (!(b is InterfaceInstance))
                        throw new Exception("Only a class instance can be compared against an interface instance");
                    InterfaceInstance ia = a as InterfaceInstance;
                    InterfaceInstance ib = b as InterfaceInstance;

                    if (operation == "==")
                        return new BoolValue(ia.Equals(ib));
                    else
                        return new BoolValue(!ia.Equals(ib));
                }
                else
                {
                    throw new Exception("Operation '" + operation + "' is not supported on interface instances");

                }
            }
            else
            {
                throw new Exception("The type " + a.GetType() + " does not support binary operators");
            }
        }

        public override string ToString()
        {
            return "(" + operand1.ToString() + " " + operation + " " + operand2.ToString() + ")";
        }

        public override IEnumerable<Expression> GetSubExpressions()
        {
            yield return operand1;
            yield return operand2;
        }
    }

    public class AnonFunExpr : Expression
    {
        public HeronFormalArgs formals;
        public CodeBlock body;
        public HeronType rettype;

        private FunctionDefinition function;

        public override HeronValue Eval(HeronExecutor vm)
        {
            FunctionValue fo = new FunctionValue(null, GetFunction());
            fo.ComputeFreeVars(vm);
            return fo;
        }

        public override string ToString()
        {
            return "function" + formals.ToString() + body.ToString();
        }

        private FunctionDefinition GetFunction()
        {
            if (function == null)
            {
                function = new FunctionDefinition(null);
                function.formals = formals;
                function.body = body;
                function.rettype = rettype;
            }
            return function;            
        }

        public override IEnumerable<Expression> GetSubExpressions()
        {
            return noExpressions;
        }
    }

    public class PostIncExpr : Expression
    {
        Expression expr;
        Assignment ass;

        public PostIncExpr(Expression x)
        {
            expr = x;
            ass = new Assignment(x, new BinaryOperator("+", x, new IntLiteral(1)));
        }

        public override HeronValue Eval(HeronExecutor vm)
        {
            HeronValue result = vm.Eval(expr);
            vm.Eval(ass);
            return result;
        }

        public override string ToString()
        {
            return expr.ToString() + "++";
        }

        public override IEnumerable<Expression> GetSubExpressions()
        {
            yield return expr;
        }
    }
}

