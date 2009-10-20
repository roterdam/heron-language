/// Heron language interpreter for Windows in C#
/// http://www.heron-language.com
/// Copyright (c) 2009 Christopher Diggins
/// Licenced under the MIT License 1.0 
/// http://www.opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    public abstract class PrimitiveValue<T> : HeronValue
    {
        T val;

        public PrimitiveValue(T x)
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
    }

    public class IntValue : PrimitiveValue<int>
    {
        public IntValue(int x)
            : base(x)
        {
        }

        public IntValue()
            : base(0)
        {
        }

        public override HeronValue InvokeUnaryOperator(string s)
        {
            switch (s)
            {
                case "-": return new IntValue(-GetValue());
                case "~": return new IntValue(~GetValue());
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by integers");
            }
        }

        public override HeronValue InvokeBinaryOperator(string s, HeronValue x)
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
                case "==": return new BoolValue(GetValue() == arg);
                case "!=": return new BoolValue(GetValue() != arg);
                case "<": return new BoolValue(GetValue() < arg);
                case ">": return new BoolValue(GetValue() > arg);
                case "<=": return new BoolValue(GetValue() <= arg);
                case ">=": return new BoolValue(GetValue() >= arg);
                case "..": throw new NotImplementedException();
                default:
                    throw new Exception("Binary operation: '" + s + "' not supported by integers");
            }
        }

        public override HeronType GetHeronType()
        {
            return HeronPrimitiveTypes.IntType;
        }
    }

    public class CharValue : PrimitiveValue<char>
    {
        public CharValue(char x)
            : base(x)
        {
        }

        public CharValue()
            : base('\0')
        {
        }

        public override HeronValue InvokeUnaryOperator(string s)
        {
            switch (s)
            {
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by chars");
            }
        }

        public override HeronValue InvokeBinaryOperator(string s, HeronValue x)
        {
            switch (s)
            {
                default:
                    throw new Exception("Binary operation: '" + s + "' not supported by chars");
            }
        }

        public override HeronType GetHeronType()
        {
            return HeronPrimitiveTypes.CharType;
        }
    }

    public class FloatValue : PrimitiveValue<float>
    {
        public FloatValue(float x)
            : base(x)
        {
        }

        public FloatValue()
            : base(0.0f)
        {
        }

        public override HeronValue InvokeUnaryOperator(string s)
        {
            switch (s)
            {
                case "-": return new FloatValue(-GetValue());
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by floats");
            }
        }

        public override HeronValue InvokeBinaryOperator(string s, HeronValue x)
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
            return HeronPrimitiveTypes.FloatType;
        }
    }

    public class BoolValue : PrimitiveValue<bool>
    {
        public BoolValue(bool x)
            : base(x)
        {
        }

        public BoolValue()
            : base(false)
        {
        }

        public override HeronValue InvokeUnaryOperator(string s)
        {
            switch (s)
            {
                case "!": return new BoolValue(!GetValue());
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by booleans");
            }
        }

        public override HeronValue InvokeBinaryOperator(string s, HeronValue x)
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
            return HeronPrimitiveTypes.BoolType;
        }
    }

    public class StringValue : PrimitiveValue<string>
    {
        public StringValue(string x)
            : base(x)
        {
        }

        public StringValue()
            : base("")
        {
        }

        public override HeronValue InvokeUnaryOperator(string s)
        {
            switch (s)
            {
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by strings");
            }
        }

        public override HeronValue InvokeBinaryOperator(string s, HeronValue x)
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
            return HeronPrimitiveTypes.StringType;
        }
    }
}
