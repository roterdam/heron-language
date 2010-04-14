using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    public class VarDesc : HeronValue
    {
        [HeronVisible] public string name;
        [HeronVisible] public HeronType type;
        [HeronVisible] public bool nullable;

        public VarDesc()
        {
        }

        public VarDesc(string name, HeronType type, bool nullable)
        {
            this.name = name;
            this.type = type;
            this.nullable = nullable;
        }

        public VarDesc(string name)
        {
            this.name = name;
            this.type = PrimitiveTypes.UnknownType;
            this.nullable = true;
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.VarDescType;
        }

        public void ResolveTypes(ModuleDefn global, ModuleDefn m)
        {
            type = type.Resolve(global, m);
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
                if (!nullable)
                    throw new Exception("Passing null to a non-nullable variable " + name); 
                else
                    return r;
            }
            else if (type != null)
            {
                r = x.As(type);
                if (r == null)
                    throw new Exception("Failed to convert variable " + x + " from a " + x.GetHeronType().name + " to " + type.name);
            }
            return r;
        }
    }
}
