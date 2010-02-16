/// Heron language interpreter for Windows in C#
/// http://www.heron-language.com
/// Copyright (c) 2009 Christopher Diggins
/// Licenced under the MIT License 1.0 
/// http://www.opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace HeronEngine
{
    /// <summary>
    /// This is the base class of ClassDefn, EnumDefn, InterfaceDefn, and ModuleDefn
    /// </summary>
    public abstract class HeronUserType : HeronType
    {
        public HeronUserType(ModuleDefn m, Type t, string name)
            : base(m, t, name)
        {
        }

        [HeronVisible]
        public abstract IEnumerable<FunctionDefn> GetAllMethods();

        [HeronVisible]
        public IEnumerable<FunctionDefn> GetMethods(string name)
        {
            foreach (FunctionDefn f in GetAllMethods())
                if (f.name == name)
                    yield return f;
        }
    }   

    /// <summary>
    /// Represents the definition of ta member field of ta Heron class. 
    /// Like "MethodInfo" in C#.
    /// </summary>
    public class FieldDefn : HeronValue
    {
        [HeronVisible]
        public string name;
        [HeronVisible]
        public HeronType type = PrimitiveTypes.AnyType;
        [HeronVisible]
        public bool nullable = false;
        [HeronVisible]
        public Expression expr;

        public void ResolveTypes(ModuleDefn m)
        {
            if (type is UnresolvedType)
                type = (type as UnresolvedType).Resolve(m);
        }

        public virtual HeronValue GetValue(HeronValue self)
        {
            return self.GetFieldOrMethod(name);        
        }

        public virtual void SetValue(HeronValue self, HeronValue x)
        {
            self.SetField(name, x);
        }

        public override string ToString()
        {
            return name + " : " + type.ToString();
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.FieldDefnType;
        }
    }

    /// <summary>
    /// Represents ta field of ta class derived from HeronValue
    /// </summary>
    public class ExposedField : FieldDefn
    {
        private FieldInfo fi;

        public ExposedField(FieldInfo fi)
        {
            this.name = fi.Name;
            this.type = DotNetClass.Create(fi.FieldType);
            this.fi = fi;
        }

        public override HeronValue GetValue(HeronValue self)
        {
            return DotNetObject.Marshal(fi.GetValue(self));
        }

        public override void SetValue(HeronValue self, HeronValue x)
        {
            fi.SetValue(self, DotNetObject.Unmarshal(fi.FieldType, x));
        }

        public override string ToString()
        {
            return fi.Name + " : " + fi.FieldType.ToString();
        }
    }
}
