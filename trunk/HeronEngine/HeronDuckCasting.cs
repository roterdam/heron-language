using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    /// <summary>
    /// This class adapts an object to an interface.
    /// </summary>
    public class DuckValue : InterfaceInstance
    {
        // TODO: I may want to change the requirement that DuckValue needs a 
        // class instances, same with interface instance
        public DuckValue(ClassInstance obj, InterfaceDefn i)
            : base(obj, i)
        {
            HeronType t = obj.GetHeronType();
            foreach (FunctionDefn f in i.GetAllMethods())
            {
                if (!obj.SupportsFunction(f))
                    throw new Exception("Failed to duck-type, object of type " + t.GetName() + " does not match interface " + i.GetName());
            }
        }
    }
}
