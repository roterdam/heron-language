using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    public abstract class Expr : HObject
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

        public abstract HObject Eval(Environment env);
    }

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
    }

    public class VarAssignment : Expr
    {
        public string name;
        public Expr rvalue; 

        public override HObject Eval(Environment env)
        {
            env.SetVar(name, rvalue);
            return HObject.Void;
        }

        public override string ToString()
        {
            return name + " = " + rvalue.ToString();
        }
    }

    public class WriteField : Expr
    {
        public string name;
        public Expr self;
        public Expr rvalue;

        public WriteField(Expr self, string name, Expr rvalue)
        {
            this.name = name;
            this.self = self;
            this.rvalue = rvalue;
        }

        public override HObject Eval(Environment env)
        {
            HObject x = self.Eval(env);
            if (!(x is Instance))
                Error("Left-side of field selector does not evaluate to a classifier instance");
            Instance i = x as Instance;              
            i.SetFieldValue(name, rvalue.Eval(env));
            return HObject.Void;
        }

        public override string ToString()
        {
            return "(" + self.ToString() + "." + name + " = " + rvalue.ToString() + ")";
        }        
    }

    public class ReadField : Expr
    {
        public string name;
        public Expr self;

        public ReadField(Expr self, string name)
        {
            this.self = self;
            this.name = name;
        }

        public override HObject Eval(Environment env)
        {
            HObject x = self.Eval(env);
            Assure(x is Instance, "left-side of field selector is not an object");
            Instance i = x as Instance;
            return i.GetFieldValue(name);
        }

        public override string ToString()
        {
            return "(" + self.ToString() + "." + name + ")";
        }
    }

    public class WriteAt : Expr
    {
        public Expr coll;
        public Expr index;
        public Expr rvalue;

        public WriteAt(Expr coll, Expr index, Expr val)
        {
            this.coll = coll;
            this.index = index;
            this.rvalue = val;
        }

        public override HObject Eval(Environment env)
        {
            HObject o = coll.Eval(env);
            HObject i = index.Eval(env);
            HObject v = rvalue.Eval(env);
            o.SetAt(i, v);
            return HObject.Void;
        }

        public override string ToString()
        {
            return coll + "[" + index.ToString() + "] = " + rvalue.ToString();
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

        public override HObject Eval(Environment env)
        {
            HObject o = coll.Eval(env);
            HObject i = index.Eval(env);
            return o.GetAt(i);
        }

        public override string ToString()
        {
            return coll + "[" + index.ToString() + "]";
        }
    }

    public class IntLiteral : Expr
    {
        IntObject val;

        public IntLiteral(int x)
        {
            val = new IntObject(x);
        }

        public override HObject Eval(Environment env)
        {
            return val;
        }

        public override string ToString()
        {
            return val.ToString();
        }
    }

    public class FloatLiteral : Expr
    {
        FloatObject val;

        public FloatLiteral(double x)
        {
            val = new FloatObject(x);
        }

        public override HObject Eval(Environment env)
        {
            return val;
        }

        public override string ToString()
        {
            return val.ToString();
        }
    }

    public class CharLiteral : Expr
    {
        CharObject val;

        public CharLiteral(char x)
        {
            val = new CharObject(x);
        }

        public override HObject Eval(Environment env)
        {
            return val;
        }

        public override string ToString()
        {
            return val.ToString();
        }
    }

    public class StringLiteral : Expr
    {
        StringObject val;

        public StringLiteral(string x)
        {
            val = new StringObject(x);
        }

        public override HObject Eval(Environment env)
        {
            return val;
        }

        public override string ToString()
        {
            return val.ToString();
        }
    }

    public class Name : Expr
    {
        public string name;

        public Name(string s)
        {
            name = s;
        }

        public override HObject Eval(Environment env)
        {
            return env.GetVar(name);
        }

        public override string ToString()
        {
            return name;
        }
    }

    public class FunCall : Expr
    {
        public Expr function;
        public ExprList args;

        public FunCall(Expr f, ExprList args)
        {
            this.function = f;
            this.args = args;
        }
        
        public override HObject Eval(Environment env)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return function.ToString() + args.ToString();
        }
    }

    public class New : Expr
    {
        public string type;
        public ExprList args;

        public New(string type, ExprList args)
        {
            this.type = type;
            this.args = args;
        }

        public override HObject Eval(Environment env)
        {
            return env.Instantiate(type);
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

        public override HObject Eval(Environment env)
        {
            switch (op)
            {
                case "-":
                    HObject o = operand.Eval(env);
                    return o.Invoke("-", new HObject[] { });
                default:
                    throw new Exception("unary expression '" + op + "' is not supported");
            }
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

        public override HObject Eval(Environment env)
        {
            Object a = operand1.Eval(env).ToDotNetObject();
            Object b = operand2.Eval(env).ToDotNetObject();

            if (a.GetType() == typeof(int))
            {
                if (b.GetType() == typeof(int))
                {
                    IntObject x = new IntObject((int)a);
                    return x.InvokeBinaryOperator(op, (int)b);
                }
                else if (b.GetType() == typeof(double))
                {
                    FloatObject x = new FloatObject((int)a);
                    return x.InvokeBinaryOperator(op, (double)b);
                }
                else
                {
                    throw new Exception("Incompatible types for binary operator " + a.GetType() + " and " + b.GetType());
                }
            }
            else if (a.GetType() == typeof(double))
            {
                if (b.GetType() == typeof(int))
                {
                    FloatObject x = new FloatObject((double)a);
                    return x.InvokeBinaryOperator(op, (int)b);
                }
                else if (b.GetType() == typeof(double))
                {
                    FloatObject x = new FloatObject((double)a);
                    return x.InvokeBinaryOperator(op, (double)b);
                }
                else
                {
                    throw new Exception("Incompatible types for binary operator " + op + " : " + a.GetType() + " and " + b.GetType());
                }
            }
            else if (a.GetType() == typeof(char))
            {
                CharObject x = new CharObject((char)a);
                if (b.GetType() != typeof(char))
                    throw new Exception("Incompatible types for binary operator " + op + " : " + a.GetType() + " and " + b.GetType());
                return x.InvokeBinaryOperator(op, (char)b);
            }
            else if (a.GetType() == typeof(string))
            {
                StringObject x = new StringObject((string)a);
                if (b.GetType() != typeof(string))
                    throw new Exception("Incompatible types for binary operator " + op + " : " + a.GetType() + " and " + b.GetType());
                return x.InvokeBinaryOperator(op, (string)b);
            }
            else if (a.GetType() == typeof(bool))
            {
                BoolObject x = new BoolObject((bool)a);
                if (b.GetType() != typeof(bool))
                    throw new Exception("Incompatible types for binary operator " + op + " : " + a.GetType() + " and " + b.GetType());
                return x.InvokeBinaryOperator(op, (bool)b);
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
