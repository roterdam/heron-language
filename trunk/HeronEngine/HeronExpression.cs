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

    public class Literal<T> : Expr
    {
        public T value;

        public Literal(T x)
        {
            value = x;
        }

        public override HObject Eval(Environment env)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return value.ToString();
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
                    break;
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
            HObject x = operand1.Eval(env);
            HObject y = operand2.Eval(env);
            return x.Invoke(op, new HObject[] { y });
        }

        public override string ToString()
        {
            return "(" + operand1.ToString() + " " + op + " " + operand2.ToString() + ")";
        }
            
    }
}
