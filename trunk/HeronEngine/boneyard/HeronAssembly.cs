using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace HeronEngine
{

    public class Args
    {
        public List<String> names;
        public List<Type> types;
    }

    class HeronAssembly
    {
        AssemblyName aName;
        AssemblyBuilder ab;
        TypeBuilder tb;
        ModuleBuilder mb;
        List<Type> types;

        HeronAssembly(string name) 
        {
            aName = new AssemblyName(name);
            ab = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.RunAndSave);

            // For a single-module assembly, the module name is usually
            // the assembly name plus an extension.
            mb = ab.DefineDynamicModule(aName.Name, aName.Name + ".dll");
        }

        void AddClass(string name) 
        {
            tb = mb.DefineType(name, TypeAttributes.Public);
        }

        void AddField(string name, Type type)
        {
            FieldBuilder fb = tb.DefineField(name, type, FieldAttributes.Public);
        }

        void AddMethod(string name, Type ret, Args args, Statement statement) 
        {
            MethodBuilder mb = tb.DefineMethod(name, MethodAttributes.Public, ret, args.types.ToArray());

            // TODO: 
        }

        void AddConstructor(Args args, Statement statement) 
        {
            ConstructorBuilder cb = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, args.types.ToArray());

            // TODO:
        }

        Type ReifyClass()
        {
            Type t = tb.CreateType();            
            types.Add(t);
            tb = null;
            return t;
        }

        void SaveAssembly(string s)
        {
            ab.Save(s);
        }
    }

/*
// The alternative approach. 

    class HeronInstance : HeronObject
    {
        HeronClass hclass;
        List<HeronObject> fieldVals;
    }

    class HeronClass
    {
        Type t;
        String name;
        AssemblyBuilder ab = new AssemblyBuilder();

        HeronClass(String name)
        {
            this.name = name;
        }

        void AddMethod(string name, IEnumerable<Type> argtypes, Statement body)
        {
            MethodBuilder mb = new MethodBuilder;
        }

        bool IsReified()
        {
            return t != null;
        }

        void Reify()
        {
            Trace.Assert(!IsReified());
            
        }
    }    
}
*/
}
