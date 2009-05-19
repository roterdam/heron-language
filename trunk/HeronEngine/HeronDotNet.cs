using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    public class HeronDotNet
    {
        public static string DotNetToHeronTypeString(Type t)
        {
            switch (t.Name)
            {
                case "int": return "Int";
                case "Int32": return "Int";
                case "char": return "Char";
                case "Char": return "Char";
                case "float": return "Float";
                case "Single": return "Float";
                case "bool": return "Bool";
                case "Boolean": return "Bool";
                case "string": return "String";
                case "String": return "String";
                case "Console": return "Console";
                case "Math": return "Math";
                case "Collection": return "Collection";
                case "Reflector": return "Reflector";
            }

            if (t.IsArray)
            {
                return "Collection";
            }

            return "Object";
        }

        public static HeronObject DotNetToHeronObject(Object o)
        {
            if (o == null)
                return HeronObject.Null;

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
                        return new FloatObject((float)o);
                    case "Double":
                        double d = (double)o;
                        // TEMP: Downcasts doubles to floats for now.
                        return new FloatObject((float)d);
                    case "Int32":
                        return new IntObject((int)o);
                    case "Char":
                        return new CharObject((char)o);
                    case "String":
                        return new StringObject((string)o);
                    case "Boolean":
                        return new BoolObject((bool)o);
                    default:
                        return DotNetObject.CreateDotNetObjectNoMarshal(o);
                }
            }
        }

        static public Object[] ObjectsToDotNetArray(HeronObject[] array)
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
