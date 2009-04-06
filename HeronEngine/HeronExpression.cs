using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

    public class VarAssignment : Expr
    {
        public string name;
        public Expr rvalue; 

        public override HeronObject Eval(Environment env)
        {
            env.SetVar(name, rvalue.Eval(env));
            return HeronObject.Void;
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

        public override HeronObject Eval(Environment env)
        {
            HeronObject x = self.Eval(env);
            if (!(x is Instance))
                Error("Left-side of field selector does not evaluate to a classifier instance");
            Instance i = x as Instance;              
            i.SetFieldValue(name, rvalue.Eval(env));
            return HeronObject.Void;
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

        public override HeronObject Eval(Environment env)
        {
            HeronObject x = self.Eval(env);
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

        public override HeronObject Eval(Environment env)
        {
            HeronObject o = coll.Eval(env);
            HeronObject i = index.Eval(env);
            HeronObject v = rvalue.Eval(env);
            o.SetAt(i, v);
            return HeronObject.Void;
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
        
        public override HeronObject Eval(Environment env)
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
        string basetype;
        public ExprList args;

        public New(string type, ExprList args)
        {
            this.type = type;
            basetype = HeronType.StripTemplateArgs(type);
            this.args = args;
        }

        public override HeronObject Eval(Environment env)
        {
            // Convert things like List<int> to List
            HeronObject var = env.GetVar(basetype);            

            if (!(var is HeronType))
                throw new Exception("only types can be instantiated, " + var + " is not a type it is a " + var.GetType());
            
            if (var is HeronClass)
            {
                HeronClass c = var as HeronClass;
                HeronObject r = c.Instantiate(env, args.Eval(env));
                return r;
            }
            else if (var is DotNetType)
            {
                DotNetType t = var as DotNetType;
                HeronObject r = t.Instantiate(env, args.Eval(env));
                return r;
            }
            else
            {
                throw new Exception("The type " + basetype + " is not recognized as an instantiable type");
            }
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
