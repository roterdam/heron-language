using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    #region primitive values
    public abstract class PrimitiveTemplate<T> : HeronValue
    {
        T val;

        public PrimitiveTemplate(T x)
        {
            val = x;
        }

        public override string ToString()
        {
            return val.ToString();
        }

        public override object ToSystemObject()
        {
            return val;
        }

        public T GetValue()
        {
            return val;
        }

        public void SetValue(T x)
        {
            val = x;
        }

        [HeronVisible]
        public HeronValue AsString()
        {
            return new StringValue(val.ToString());
        }

        [HeronVisible]
        public override int GetHashCode()
        {
            return val.GetHashCode();
        }

        [HeronVisible]
        public override bool Equals(object obj)
        {
            if (obj is PrimitiveTemplate<T>)
            {
                return (obj as PrimitiveTemplate<T>).GetValue().Equals(GetValue());
            }
            else
            {
                return false;
            }
        }
    }

    public class IntValue : PrimitiveTemplate<int>
    {
        public IntValue(int x)
            : base(x)
        {
        }

        public IntValue()
            : base(0)
        {
        }

        public override int ToInt()
        {
            return GetValue();
        }

        public override HeronValue InvokeUnaryOperator(VM vm, string s)
        {
            switch (s)
            {
                case "-": return vm.MakeTemporary(-GetValue());
                case "~": return vm.MakeTemporary(~GetValue());
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by integers");
            }
        }

        public override HeronValue InvokeBinaryOperator(VM vm, OpCode opcode, HeronValue val)
        {
            int x = GetValue();
            int y = val.ToInt();

            switch (opcode)
            {
                case OpCode.opAdd: return vm.MakeTemporary(x + y);
                case OpCode.opSub: return vm.MakeTemporary(x - y);
                case OpCode.opMul: return vm.MakeTemporary(x * y);
                case OpCode.opDiv: return vm.MakeTemporary(x / y);
                case OpCode.opMod: return vm.MakeTemporary(x % y);
                case OpCode.opShr: return vm.MakeTemporary(x >> y);
                case OpCode.opShl: return vm.MakeTemporary(x << y);
                case OpCode.opGtEq: return vm.MakeTemporary(x >= y);
                case OpCode.opLtEq: return vm.MakeTemporary(x <= y);
                case OpCode.opGt: return vm.MakeTemporary(x > y);
                case OpCode.opLt: return vm.MakeTemporary(x < y);
                case OpCode.opRange: return new RangeEnumerator(this as IntValue, val as IntValue);
                default: return base.InvokeBinaryOperator(vm, opcode, val);
            }
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.IntType;
        }
    }

    public class CharValue : PrimitiveTemplate<char>
    {
        public CharValue(char x)
            : base(x)
        {
        }

        public CharValue()
            : base('\0')
        {
        }

        public override char ToChar()
        {
            return GetValue();
        }

        public override HeronValue InvokeUnaryOperator(VM vm, string s)
        {
            switch (s)
            {
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by chars");
            }
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.CharType;
        }
    }

    public class FloatValue : PrimitiveTemplate<float>
    {
        public FloatValue(float x)
            : base(x)
        {
        }

        public FloatValue()
            : base(0.0f)
        {
        }

        public override float ToFloat()
        {
            return GetValue();
        }

        public override HeronValue InvokeUnaryOperator(VM vm, string s)
        {
            switch (s)
            {
                case "-": return vm.MakeTemporary(-GetValue());
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by floats");
            }
        }

        public override HeronValue InvokeBinaryOperator(VM vm, OpCode opcode, HeronValue val)
        {
            float x = GetValue();
            float y = val.ToFloat();

            switch (opcode)
            {
                case OpCode.opAdd: return vm.MakeTemporary(x + y);
                case OpCode.opSub: return vm.MakeTemporary(x - y);
                case OpCode.opMul: return vm.MakeTemporary(x * y);
                case OpCode.opDiv: return vm.MakeTemporary(x / y);
                case OpCode.opMod: return vm.MakeTemporary(x % y);
                case OpCode.opGtEq: return vm.MakeTemporary(x >= y);
                case OpCode.opLtEq: return vm.MakeTemporary(x <= y);
                case OpCode.opGt: return vm.MakeTemporary(x > y);
                case OpCode.opLt: return vm.MakeTemporary(x < y);
                default: return base.InvokeBinaryOperator(vm, opcode, val);
            }
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.FloatType;
        }
    }

    public class BoolValue : PrimitiveTemplate<bool>
    {
        public BoolValue(bool x)
            : base(x)
        {
        }

        public BoolValue()
            : base(false)
        {
        }

        public override HeronValue InvokeUnaryOperator(VM vm, string s)
        {
            switch (s)
            {
                case "!": return vm.MakeTemporary(!GetValue());
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by booleans");
            }
        }

        public override HeronValue InvokeBinaryOperator(VM vm, OpCode opcode, HeronValue val)
        {
            bool x = GetValue();
            bool y = val.ToBool();

            switch (opcode)
            {
                case OpCode.opAnd: return vm.MakeTemporary(x && y);
                case OpCode.opOr: return vm.MakeTemporary(x || y);
                case OpCode.opXOr: return vm.MakeTemporary(x ^ y);
                default: return base.InvokeBinaryOperator(vm, opcode, val);
            }
        }
 
        public override bool ToBool()
        {
            return GetValue();
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.BoolType;
        }

        public override string ToString()
        {
            return GetValue() ? "true" : "false";
        }
    }

    public class StringValue : PrimitiveTemplate<string>
    {
        public StringValue(string x)
            : base(x)
        {
        }

        public StringValue()
            : base("")
        {
        }

        public override HeronValue InvokeUnaryOperator(VM vm, string s)
        {
            switch (s)
            {
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by strings");
            }
        }

        public override HeronValue InvokeBinaryOperator(VM vm, OpCode opcode, HeronValue val)
        {
            if (opcode == OpCode.opAdd)
            {
                return vm.MakeTemporary(GetValue() + val.ToString());
            }
            else
            {
                return base.InvokeBinaryOperator(vm, opcode, val);
            }
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.StringType;
        }

        [HeronVisible]
        public HeronValue Length()
        {
            return new IntValue(GetValue().Length);
        }

        [HeronVisible]
        public HeronValue GetChar(IntValue index)
        {
            return new CharValue(GetValue()[index.GetValue()]);
        }
    }
    #endregion
}
