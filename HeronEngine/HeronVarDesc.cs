using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    public class VarDesc : HeronValue
    {
        [HeronVisible] public string name;
        [HeronVisible] public TypeRef type;

        public VarDesc()
        {
        }

        public VarDesc(string name, TypeRef type)
        {
            this.name = name;
            this.type = type;
        }

        public VarDesc(string name)
        {
            this.name = name;
            this.type = new TypeRef("Void", true);
        }

        public override HeronType Type
        {
            get { return PrimitiveTypes.VarDescType; }
        }

        public void ResolveTypes(ModuleDefn global, ModuleDefn m)
        {
            type.Resolve(global, m);
        }

        /// <summary>
        /// Converts a HeronValue into the expected type. Checks for legal null
        /// passing (i.e. assures only nullable types receive nulls), and does type-checking. 
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public HeronValue Coerce(HeronValue x)
        {
            HeronValue r = x;
            if (r is NullValue)
            {
                if (!type.nullable)
                    throw new Exception("Passing null to a non-nullable variable " + name); 
                else
                    return r;
            }
            else if (type != null)
            {
                r = x.As(type.type);
                if (r == null)
                    throw new Exception("Failed to convert variable " + x + " from a " + x.Type.name + " to " + type.name);
            }
            return r;
        }
    }
}
