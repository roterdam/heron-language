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

        public override HeronValue InvokeUnaryOperator(VM vm, string s)
        {
            switch (s)
            {
                case "-": return new IntValue(-GetValue());
                case "~": return new IntValue(~GetValue());
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by integers");
            }
        }

        public override HeronValue InvokeBinaryOperator(VM vm, string s, HeronValue x)
        {
            if (!(x is IntValue))
                throw new Exception("binary operation not supported on differently typed objects");

            int arg = (x as IntValue).GetValue();
            switch (s)
            {
                case "+": return new IntValue(GetValue() + arg);
                case "-": return new IntValue(GetValue() - arg);
                case "*": return new IntValue(GetValue() * arg);
                case "/": return new IntValue(GetValue() / arg);
                case "%": return new IntValue(GetValue() % arg);
                case ">>": return new IntValue(GetValue() >> arg);
                case "<<": return new IntValue(GetValue() << arg);
                case "==": return new BoolValue(GetValue() == arg);
                case "!=": return new BoolValue(GetValue() != arg);
                case "<": return new BoolValue(GetValue() < arg);
                case ">": return new BoolValue(GetValue() > arg);
                case "<=": return new BoolValue(GetValue() <= arg);
                case ">=": return new BoolValue(GetValue() >= arg);
                case "..": return new RangeEnumerator(this, x as IntValue);
                default:
                    throw new Exception("Binary operation: '" + s + "' not supported by integers");
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

        public override HeronValue InvokeUnaryOperator(VM vm, string s)
        {
            switch (s)
            {
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by chars");
            }
        }

        public override HeronValue InvokeBinaryOperator(VM vm, string s, HeronValue x)
        {
            char arg = (x as CharValue).GetValue();
            switch (s)
            {
                case "==": return new BoolValue(GetValue() == arg);
                case "!=": return new BoolValue(GetValue() != arg);
                default:
                    throw new Exception("Binary operation: '" + s + "' not supported by chars");
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

        public override HeronValue InvokeUnaryOperator(VM vm, string s)
        {
            switch (s)
            {
                case "-": return new FloatValue(-GetValue());
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by floats");
            }
        }

        public override HeronValue InvokeBinaryOperator(VM vm, string s, HeronValue x)
        {
            if (!(x is FloatValue))
                throw new Exception("binary operation not supported on differently typed objects");
            float arg = (x as FloatValue).GetValue();
            switch (s)
            {
                case "+": return new FloatValue(GetValue() + arg);
                case "-": return new FloatValue(GetValue() - arg);
                case "*": return new FloatValue(GetValue() * arg);
                case "/": return new FloatValue(GetValue() / arg);
                case "%": return new FloatValue(GetValue() % arg);
                case "==": return new BoolValue(GetValue() == arg);
                case "!=": return new BoolValue(GetValue() != arg);
                case "<": return new BoolValue(GetValue() < arg);
                case ">": return new BoolValue(GetValue() > arg);
                case "<=": return new BoolValue(GetValue() <= arg);
                case ">=": return new BoolValue(GetValue() >= arg);
                default:
                    throw new Exception("Binary operation: '" + s + "' not supported by floats");
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
                case "!": return new BoolValue(!GetValue());
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by booleans");
            }
        }

        public override HeronValue InvokeBinaryOperator(VM vm, string s, HeronValue x)
        {
            if (!(x is BoolValue))
                throw new Exception("binary operation not supported on differently typed objects");
            bool arg = (x as BoolValue).GetValue();
            switch (s)
            {
                case "==": return new BoolValue(GetValue() == arg);
                case "!=": return new BoolValue(GetValue() != arg);
                case "&&": return new BoolValue(GetValue() && arg);
                case "||": return new BoolValue(GetValue() || arg);
                case "^^": return new BoolValue(GetValue() ^ arg);
                default:
                    throw new Exception("Binary operation: '" + s + "' not supported by booleans");
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

        public override HeronValue InvokeBinaryOperator(VM vm, string s, HeronValue x)
        {
            if (!(x is StringValue))
                throw new Exception("binary operation not supported on differently typed objects");
            StringValue so = x as StringValue;
            string arg = (x as StringValue).GetValue();
            switch (s)
            {
                case "+": return new StringValue(GetValue() + arg);
                case "==": return new BoolValue(GetValue() == so.GetValue());
                case "!=": return new BoolValue(GetValue() != so.GetValue());
                default:
                    throw new Exception("Binary operation: '" + s + "' not supported by strings");
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
