using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace HeronEngine
{
    /// <summary>
    /// This is used to create reflector objects.
    /// A reflector object exposes part of the vm to 
    /// Heron. We don't expose the vm directly because
    /// it exposes stuff used by the engine
    /// </summary>
    public static class HeronReflection
    {
        static HeronExecutor vm;

        public static void SetVM(HeronExecutor vm)
        {
            HeronReflection.vm = vm;
        }

        public static string GetCurrentFunctionName()
        {
            Frame f = vm.GetCurrentFrame();
            return f.function.name;
        }

        public static void ToArray(HeronValue o, string s)
        {
            Assembly a = Assembly.GetExecutingAssembly();
            Type t = a.GetType(s);

            // TODO: create a special HeronCollectionObject 
            // that derives from .NET object (so that it can have methods).
            // I think? I mean.

            throw new NotImplementedException();
        }
    }
}
