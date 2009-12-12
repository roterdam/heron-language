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
    public class AnyValue : HeronValue
    {
        HeronValue obj;
        HeronType type;

        public AnyValue(HeronValue obj)
        {
            if (obj is AnyValue)
            {
                this.obj = (obj as AnyValue).obj;
                this.type = (obj as AnyValue).type;
            }
            else
            {
                this.obj = obj;
                this.type = obj.GetHeronType();
            }
        }

        public bool Is(HeronType t)
        {
            if (type.name == t.name)
                return true;

            if (type is HeronClass)
            {
                HeronClass c = type as HeronClass;

                if (t is HeronClass)
                {
                    return c.InheritsFrom(t as HeronClass);
                }
                else if (t is HeronInterface)
                {
                    return c.Implements(t as HeronInterface);
                }
            }
            else if (type is HeronInterface)
            {
                HeronInterface i = type as HeronInterface;
                
                if (t is HeronInterface)
                    return i.InheritsFrom(t as HeronInterface);
            }

            return false;
        }

        public HeronValue As(HeronType t)
        {
            if (type.name == t.name)
                return obj;

            if (type is HeronClass)
            {
                ClassInstance inst = obj as ClassInstance;
                if (inst == null)
                    throw new Exception("Expected an instance of a class");
                return inst.As(t);
            }
            else if (type is HeronInterface)
            {
                InterfaceInstance ii = obj as InterfaceInstance;
                if (ii == null)
                    throw new Exception("Expected an instance of an interface");
                HeronInterface i = type as HeronInterface;

                if (t is HeronInterface) 
                    if (i.InheritsFrom(t as HeronInterface))
                        return new InterfaceInstance(ii.GetObject(), t as HeronInterface);
            }
            else if (type is DotNetClass)
            {
                if (!(t is DotNetClass))
                    throw new Exception("External objects can only be cast to the type 'DotNetClass'");

                if (!t.Equals(type))
                    throw new Exception("Cannot convert from '" + type.name + "' to '" + t.name);

                return obj;
            }
            else if (t.name == "Any")
            {
                return this;
            }
            else
            {
                Type from = type.GetSystemType();
                Type to = t.GetSystemType();

                if (from != null && to != null && to.IsAssignableFrom(from))
                    return obj;
            }

            throw new Exception("Cannot convert from '" + type.name + "' to '" + t.name);
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.AnyType;
        }

        public HeronType GetHeldType()
        {
            return type;
        }
    }
}
