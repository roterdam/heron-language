using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    public class HeronDotNet
    {
        public static string RenameType(string s)
        {
            switch (s)
            {
                case "int": return "Int";
                case "char": return "Char";
                case "float": return "Float";
                case "bool": return "Bool";
                case "string": return "String";
                case "Object": return "Any";
                default: return s;                    
            }
        }

        public static string DotNetToHeronTypeString(Type t)
        {
            string s = RenameType(t.Name);

            if (t.IsArray)
                return "Array<" + s + ">";

            if (t.IsGenericType)
            {
                Type[] ts = t.GetGenericArguments();
                if (ts.Length == 0)
                    return s;
                s += "<";
                for (int i = 0; i < ts.Length; ++i)
                {
                    if (i > 0) s += ", ";
                    s += DotNetToHeronTypeString(ts[i]);
                }
                s += ">";
            }

            return s;
        }

        public static HeronValue DotNetToHeronObject(Object o)
        {
            if (o == null)
                return HeronValue.Null;

            Type t = o.GetType();
            
            if (t.IsArray)
            {
                HeronCollection c = new HeronCollection();
                Array a = o as Array;
                foreach (Object e in a)
                    c.Add(DotNetToHeronObject(e));
                return DotNetObject.CreateDotNetObjectNoMarshal(c);
            }
            else
            {
                switch (o.GetType().Name)
                {
                    case "Single":
                        return new FloatValue((float)o);
                    case "Double":
                        double d = (double)o;
                        // TEMP: Downcasts doubles to floats for now.
                        return new FloatValue((float)d);
                    case "Int32":
                        return new IntValue((int)o);
                    case "Char":
                        return new CharValue((char)o);
                    case "String":
                        return new StringValue((string)o);
                    case "Boolean":
                        return new BoolValue((bool)o);
                    default:
                        return DotNetObject.CreateDotNetObjectNoMarshal(o);
                }
            }
        }

        static public Object[] ObjectsToDotNetArray(HeronValue[] array)
        {
            Object[] r = new Object[array.Length];
            for (int i = 0; i < array.Length; ++i)
            {
                if (array[i] == null)
                    r[i] = null;
                else
                    r[i] = array[i].ToSystemObject();
            }
            return r;
        }
    }
}
