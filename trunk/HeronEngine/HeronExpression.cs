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
    public abstract class Expr
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

        public void Trace()
        {
            HeronDebugger.TraceExpr(this);
        }
    }

    /// <summary>
    /// A list of expressions, used primarily for passing arguments to functions
    /// </summary>
    public class ExprList : List<Expr>
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
            foreach (Expr x in this)
                list.Add(x.Eval(env));
            return list.ToArray();
        }
    }

    public class Assignment : Expr
    {
        public Expr lvalue;
        public Expr rvalue;

        public Assignment(Expr lvalue, Expr rvalue)
        {
            this.lvalue = lvalue;
            this.rvalue = rvalue;
        }

        public override HeronObject Eval(Environment env)
        {            
            Trace();
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
                    throw new Exception(name + " is not a member field or local variable");
                }
            }
            else if (lvalue is SelectField)
            {
                SelectField sf = lvalue as SelectField;
                HeronObject self = sf.self.Eval(env);
                string name = sf.name;
                self.SetField(name, self);
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

    public class SelectField : Expr
    {
        public string name;
        public Expr self;

        public SelectField(Expr self, string name)
        {
            this.self = self;
            this.name = name;
        }

        public override HeronObject Eval(Environment env)
        {
            HeronObject x = self.Eval(env);
            return x.GetField(name);
        }

        public override string ToString()
        {
            return "(" + self.ToString() + "." + name + ")";
        }
    }

    public class ReadAt : Expr
    {
        public Expr coll;
        public Expr index;

        public ReadAt(Expr coll, Expr index)
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

    public class New : Expr
    {
        string type;
        ExprList args;

        public New(string type, ExprList args)
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

    public class Literal<T> : Expr where T : HeronObject
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
        public FloatLiteral(double x)
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

    public class Name : Expr
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
            if (r == null)
                throw new Exception("Could not find the name " + name + " in the environment");
            return r;
        }

        public override string ToString()
        {
            return name;
        }
    }

    public class FunCall : Expr
    {
        public Expr funexpr;
        public ExprList args;

        public FunCall(Expr f, ExprList args)
        {
            funexpr = f;
            this.args = args;
        }
        
        public override HeronObject Eval(Environment env)
        {
            Trace();
            HeronObject[] argvals = args.Eval(env);
            HeronObject f = funexpr.Eval(env);
            return f.Apply(env, argvals);
        }

        public override string ToString()
        {
            return funexpr.ToString() + args.ToString();
        }
    }


    public class UnaryOperator : Expr
    {
        public Expr operand;
        public string op;

        public UnaryOperator(string sOp, Expr x)
        {
            op = sOp;
            operand = x;
        }

        public override HeronObject Eval(Environment env)
        {
            HeronObject o = operand.Eval(env);
            return o.InvokeUnaryOperator(op);
        }

        public override string ToString()
        {
            return "(" + op + "  " + operand.ToString() + ")";
        }
    }

    public class BinaryOperator : Expr
    {
        public Expr operand1;
        public Expr operand2;
        public string op;

        public BinaryOperator(string sOp, Expr x, Expr y)
        {
            op = sOp;
            operand1 = x;
            operand2 = y;
        }

        public override HeronObject Eval(Environment env)
        {
            HeronObject a = operand1.Eval(env);
            HeronObject b = operand2.Eval(env);

            if (a is IntObject)
            {
                if (b is IntObject)
                {
                    return (a as IntObject).InvokeBinaryOperator(op, b as IntObject);
                }
                else if (b is FloatObject)
                {
                    return (new FloatObject((a as IntObject).GetValue())).InvokeBinaryOperator(op, b as FloatObject);
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
                    return (a as FloatObject).InvokeBinaryOperator(op, new FloatObject((b as IntObject).GetValue()));
                }
                else if (b is FloatObject)
                {
                    return (a as FloatObject).InvokeBinaryOperator(op, b as FloatObject);
                }
                else
                {
                    throw new Exception("Incompatible types for binary operator " + op + " : " + a.GetType() + " and " + b.GetType());
                }
            }
            else if (a is CharObject)
            {
                if (!(b is CharObject))
                    throw new Exception("Incompatible types for binary operator " + op + " : " + a.GetType() + " and " + b.GetType());
                return (a as CharObject).InvokeBinaryOperator(op, b as CharObject);
            }
            else if (a is StringObject)
            {
                if (!(b is StringObject))
                    throw new Exception("Incompatible types for binary operator " + op + " : " + a.GetType() + " and " + b.GetType());
                return (a as StringObject).InvokeBinaryOperator(op, b as StringObject);
            }
            else if (a is BoolObject)
            {
                if (!(b is BoolObject))
                    throw new Exception("Incompatible types for binary operator " + op + " : " + a.GetType() + " and " + b.GetType());
                return (a as BoolObject).InvokeBinaryOperator(op, b as BoolObject);
            }
            else
            {
                throw new Exception("The type " + a.GetType() + " does not support binary operators");
            }
        }

        public override string ToString()
        {
            return "(" + operand1.ToString() + " " + op + " " + operand2.ToString() + ")";
        }
            
    }
}
