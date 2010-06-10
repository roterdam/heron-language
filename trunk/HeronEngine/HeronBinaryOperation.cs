using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace HeronEngine
{
    delegate HeronValue BinaryOperator(HeronValue a, HeronValue b);

    public enum OpCode
    {
        opAdd,
        opSub,
        opMul,
        opDiv,
        opMod,
        opShr,
        opShl,
        opGtEq,
        opGt,
        opLtEq,
        opLt,
        opRange,
        opEq,
        opNEq,
        opIs,
        opAs,
        opAnd,
        opOr,
        opXOr,
    };

    class BinaryOperation : Expression
    {
        public static OpCode StringToOpCode(string s)
        {
            switch (s)
            {
                case "+": return OpCode.opAdd;
                case "-": return OpCode.opSub;
                case "*": return OpCode.opMul;
                case "/": return OpCode.opDiv;
                case "%": return OpCode.opMod;
                case ">>": return OpCode.opShr;
                case "<<": return OpCode.opShl;
                case ">=": return OpCode.opGtEq;
                case ">": return OpCode.opGt;
                case "<=": return OpCode.opLtEq;
                case "<": return OpCode.opLt;
                case "..": return OpCode.opRange;
                case "==": return OpCode.opEq;
                case "!=": return OpCode.opNEq;
                case "is": return OpCode.opIs;
                case "as": return OpCode.opAs;
                case "&&": return OpCode.opAnd;
                case "||": return OpCode.opOr;
                case "^^": return OpCode.opXOr;
            }
            throw new Exception("Unrecognized operation " + s);
        }

        [HeronVisible] public Expression operand1;
        [HeronVisible] public Expression operand2;
        [HeronVisible] public string operation;
        public OpCode opcode;

        public BinaryOperation(string sOp, Expression x, Expression y)
        {
            operation = sOp;
            opcode = StringToOpCode(sOp);
            operand1 = x;
            operand2 = y;
        }


        public override HeronValue Eval(VM vm)
        {
            HeronValue a = operand1.Eval(vm);
            HeronValue b = operand2.Eval(vm);

            switch (opcode)
            {
                case OpCode.opEq:
                    return new BoolValue(a.Equals(b));
                case OpCode.opNEq:
                    return new BoolValue(!a.Equals(b));
                case OpCode.opIs:
                    {
                        TypeValue tv = b as TypeValue;
                        if (tv == null)
                            throw new Exception("The second argument of the 'is' operator must be a type");
                        return new BoolValue(a.Is(tv.Type));
                    }
                case OpCode.opAs:
                    {
                        HeronType t = b as HeronType;
                        if (t == null)
                            throw new Exception("The 'as' operator expects a type as a right hand argument");
                        HeronValue r = a.As(t);
                        if (r != null)
                            return r;
                        if (t is InterfaceDefn && a is ClassInstance)
                        {
                            DuckValue dv = new DuckValue(a as ClassInstance, t as InterfaceDefn);
                            return dv;
                        }
                        throw new Exception("Failed to convert " + a.Type.name + " to a " + t.name);
                    };
            }

            return a.InvokeBinaryOperator(vm, opcode, b);
        }

        public override string ToString()
        {
            return "(" + operand1.ToString() + " " + operation + " " + operand2.ToString() + ")";
        }

        public override HeronType Type
        {
            get { return PrimitiveTypes.BinaryOperation; }
        }

        public override Expression Optimize(OptimizationParams op)
        {
            return new BinaryOperation(operation, operand1.Optimize(op), operand2.Optimize(op));
        }
    }
}
