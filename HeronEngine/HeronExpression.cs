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

        public abstract HeronObject Eval(Environment env);
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

        public HeronObject[] Eval(Environment env)
        {
            List<HeronObject> list = new List<HeronObject>();
            foreach (Expression x in this)
                list.Add(x.Eval(env));
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

        public override HeronObject Eval(Environment env)
        {            
            HeronObject val = rvalue.Eval(env);

            if (lvalue is Name)
            {
                string name = (lvalue as Name).name;
                if (env.HasVar(name))
                {
                    env.SetVar(name, val);
                    return HeronObject.Void;
                }
                else if (env.HasField(name))
                {
                    env.SetField(name, val);
                    return HeronObject.Void;
                }
                else
                {
                    throw new Exception(name + " is not a member field or local variable that can be assigned to");
                }
            }
            else if (lvalue is SelectField)
            {
                SelectField sf = lvalue as SelectField;
                HeronObject self = sf.self.Eval(env);
                string name = sf.name;
                self.SetField(name, val);
                return HeronObject.Void;
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

        public override HeronObject Eval(Environment env)
        {
            HeronObject x = self.Eval(env);
            if (x == null)
                throw new Exception("Cannot select field '" + name + "' from a null object: " + self.ToString());
            return x.GetFieldOrMethod(name);
        }

        public override string ToString()
        {
            return "(" + self.ToString() + "." + name + ")";
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

        public override HeronObject Eval(Environment env)
        {
            HeronObject o = coll.Eval(env);
            HeronObject i = index.Eval(env);
            return o.GetAt(i);
        }

        public override string ToString()
        {
            return coll + "[" + index.ToString() + "]";
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

        public override HeronObject Eval(Environment env)
        {
            HeronObject o = env.LookupName(type);
            if (!(o is HeronType))
                throw new Exception("Cannot instantiate non-type " + type);
            HeronType t = o as HeronType;
            HeronObject[] argvals = args.Eval(env);
            return t.Instantiate(env, argvals);
        }
    }

    public abstract class Literal<T> : Expression where T : HeronObject
    {
        T val;

        public Literal(T x)
        {
            val = x;
        }

        public override HeronObject Eval(Environment env)
        {
            return val;
        }

        public override string ToString()
        {
            return val.ToString();
        }
    }

    public class IntLiteral : Literal<IntObject>
    {
        public IntLiteral(int x)
            : base(new IntObject(x))
        {
        }
    }

    public class FloatLiteral : Literal<FloatObject>
    {
        public FloatLiteral(float x)
            : base(new FloatObject(x))
        {
        }
    }

    public class CharLiteral : Literal<CharObject> 
    {
        public CharLiteral(char x)
            : base(new CharObject(x))
        {
        }
    }

    public class StringLiteral : Literal<StringObject>
    {
        public StringLiteral(string x)
            : base(new StringObject(x))
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

        public override HeronObject Eval(Environment env)
        {
            string s = env.ToString();
            HeronObject r = env.LookupName(name);
            return r;
        }

        public override string ToString()
        {
            return name;
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
        
        public override HeronObject Eval(Environment env)
        {
            HeronObject[] argvals = args.Eval(env);
            HeronObject f = funexpr.Eval(env);
            return f.Apply(env, argvals);
        }

        public override string ToString()
        {
            return funexpr.ToString() + args.ToString();
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

        public override HeronObject Eval(Environment env)
        {
            HeronObject o = operand.Eval(env);
            return o.InvokeUnaryOperator(operation);
        }

        public override string ToString()
        {
            return "(" + operation + "  " + operand.ToString() + ")";
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

        public override HeronObject Eval(Environment env)
        {
            HeronObject a = operand1.Eval(env);
            HeronObject b = operand2.Eval(env);

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

                return new BoolObject(any.Is(b as HeronType));                    
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
            else if (a is IntObject)
            {
                if (b is IntObject)
                {
                    return a.InvokeBinaryOperator(operation, b as IntObject);
                }
                else if (b is FloatObject)
                {
                    return (new FloatObject((a as IntObject).GetValue())).InvokeBinaryOperator(operation, b as FloatObject);
                }
                else
                {
                    throw new Exception("Incompatible types for binary operator " + a.GetType() + " and " + b.GetType());
                }
            }
            else if (a is FloatObject)
            {
                if (b is IntObject)
                {
                    return a.InvokeBinaryOperator(operation, new FloatObject((b as IntObject).GetValue()));
                }
                else if (b is FloatObject)
                {
                    return a.InvokeBinaryOperator(operation, b as FloatObject);
                }
                else
                {
                    throw new Exception("Incompatible types for binary operator " + operation + " : " + a.GetType() + " and " + b.GetType());
                }
            }
            else if (a is CharObject)
            {
                if (!(b is CharObject))
                    throw new Exception("Incompatible types for binary operator " + operation + " : " + a.GetType() + " and " + b.GetType());
                return a.InvokeBinaryOperator(operation, b as CharObject);
            }
            else if (a is StringObject)
            {
                if (!(b is StringObject))
                    throw new Exception("Incompatible types for binary operator " + operation + " : " + a.GetType() + " and " + b.GetType());
                return a.InvokeBinaryOperator(operation, b as StringObject);
            }
            else if (a is BoolObject)
            {
                if (!(b is BoolObject))
                    throw new Exception("Incompatible types for binary operator " + operation + " : " + a.GetType() + " and " + b.GetType());
                return a.InvokeBinaryOperator(operation, b as BoolObject);
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
                        return new BoolObject(ea.Equals(eb));
                    else
                        return new BoolObject(!ea.Equals(eb));
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
                        return new BoolObject(ca.Equals(cb));
                    else
                        return new BoolObject(!ca.Equals(cb));
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
                        return new BoolObject(ia.Equals(ib));
                    else
                        return new BoolObject(!ia.Equals(ib));
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
    }

    public class AnonFunExpr : Expression
    {
        public HeronFormalArgs formals;
        public CodeBlock body;
        public HeronType rettype;

        private HeronFunction function;

        public override HeronObject Eval(Environment env)
        {
            FunctionObject fo = new FunctionObject(null, GetFunction());
            return fo;
        }

        public override string ToString()
        {
            return "function" + formals.ToString() + body.ToString();
        }

        private HeronFunction GetFunction()
        {
            if (function == null)
            {
                function = new HeronFunction(null);
                function.formals = formals;
                function.body = body;
                function.rettype = rettype;
            }
            return function;
            
        }
    }
}

