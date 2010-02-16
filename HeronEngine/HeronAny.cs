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
            AnyValue av = (obj as AnyValue);
            if (av != null)
            {
                this.obj = av.obj;
                this.type = av.type;
            }
            else
            {
                this.obj = obj;
                this.type = obj.GetHeronType();
            }
        }

        public override HeronValue As(HeronType t)
        {
            if (type.name == t.name)
                return obj;

            if (type is ClassDefn)
            {
                ClassInstance inst = obj as ClassInstance;
                if (inst == null)
                    throw new Exception("Expected an instance of a class");
                return inst.As(t);
            }
            else if (type is InterfaceDefn)
            {
                InterfaceInstance ii = obj as InterfaceInstance;
                if (ii == null)
                    throw new Exception("Expected an instance of an interface");
                return ii.As(t);
            }
            else if (type is DotNetClass)
            {
                if (!(t is DotNetClass))
                    throw new Exception("External objects can only be cast to the type 'DotNetClass'");

                if (t.Equals(type))
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

            return null;
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
