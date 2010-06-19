using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    public class TypeRef : HeronValue
    {
        [HeronVisible] public List<TypeRef> args = new List<TypeRef>();
        [HeronVisible] public string name;
        [HeronVisible] public bool nullable;
        [HeronVisible] public HeronType type;

        public TypeRef() : this("Void") { }
        public TypeRef(string name) : this(name, false) { }
        public TypeRef(string name, bool nullable) : this(name, nullable, new TypeRef[] { }) { }
        public TypeRef(string name, bool nullable, params TypeRef[] args) : this(name, nullable, new List<TypeRef>(args)) { } 
        public TypeRef(string name, bool nullable, List<TypeRef> args) { this.name = name; this.nullable = nullable; this.args = args; }  
        public TypeRef(HeronType t) : this(t.name) { type = t; }
        public TypeRef(HeronType t, bool nullable) : this(t.name, nullable) { type = t; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            ToString(sb);
            return sb.ToString();
        }

        public void ToString(StringBuilder sb)
        {
            sb.Append(name);
            if (args.Count > 0)
            {
                sb.Append('<');
                for (int i = 0; i < args.Count; ++i)
                {
                    if (i > 0) sb.Append(',');
                    args[i].ToString(sb);
                }
                sb.Append('>');
            }
            if (nullable)
                sb.Append('?');
        }

        public override HeronType Type
        {
            get { return PrimitiveTypes.TypeRefType; }
        }

        public void Resolve(ModuleDefn global, ModuleDefn m)
        {
            type = m.FindType(name);
            if (type == null)
                type = global.FindType(name);
            if (type == null)
                throw new Exception("Could not resolve type " + name);
            if (type.name != name)
                throw new Exception("Internal error during type resolution of " + name);
        }
    }
}
