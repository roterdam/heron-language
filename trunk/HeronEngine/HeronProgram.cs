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
using System.IO;

namespace HeronEngine
{
    /// <summary>
    /// Represents an executable Heron program.
    /// </summary>
    public class HeronProgram 
    {
        private List<HeronModule> modules = new List<HeronModule>();
        private HeronModule global;
        public string name;

        public HeronProgram(string name)
        {
            this.name = name;
            global = new HeronModule(this, "_global_");
            RegisterPrimitives();
        }

        internal void AddModule(HeronModule m)
        {
            modules.Add(m);
        }

        public HeronModule GetGlobal()
        {
            return global;
        }

        public IEnumerable<HeronModule> GetModules()
        {
            return modules;
        }
        internal void RegisterDotNetType(Type t, string name)
        {
            global.AddDotNetType(name, t);
        }

        internal void RegisterDotNetType(Type t)
        {
            global.AddDotNetType(t.Name, t);
        }

        public void LoadAssembly(string s)
        {
            Assembly a = null;
            foreach (String tmp in Config.libraryPath)
            {
                string path = tmp + "//" + s;
                if (File.Exists(path)) {
                    a = Assembly.LoadFrom(path);
                    break;
                }
                path += ".dll";
                if (File.Exists(path))
                {
                    a = Assembly.LoadFrom(path);
                    break;
                }
            }
            if (a == null)
                throw new Exception("Could not find assembly " + s);
            foreach (Type t in a.GetExportedTypes())
                RegisterDotNetType(t);
        }

        /// <summary>
        /// This exposes a set of globally recognized Heron and .NET 
        /// types to the environment (essentially global variables).
        /// A simple way to extend the scope of Heron is to introduce
        /// new types in this function.
        /// </summary>
        void RegisterPrimitives()
        {
            Dictionary<string, HeronPrimitiveType> prims = HeronPrimitiveTypes.GetTypes();
            foreach (string s in prims.Keys)
                global.AddPrimitive(s, prims[s]);

            RegisterDotNetType(typeof(Console), "Console");
            RegisterDotNetType(typeof(Math), "Math");

            RegisterDotNetType(typeof(VariableDeclaration));
            RegisterDotNetType(typeof(DeleteStatement));
            RegisterDotNetType(typeof(ExpressionStatement));
            RegisterDotNetType(typeof(ForEachStatement));
            RegisterDotNetType(typeof(ForStatement));
            RegisterDotNetType(typeof(CodeBlock));
            RegisterDotNetType(typeof(IfStatement));
            RegisterDotNetType(typeof(WhileStatement));
            RegisterDotNetType(typeof(ReturnStatement));

            // Load other libraries specified in the configuration file
            foreach (string lib in Config.libs)
                LoadAssembly(lib);

            // Load the standard library types
            RegisterDotNetType(typeof(HeronStandardLibrary.Viewport), "Viewport");
            RegisterDotNetType(typeof(HeronStandardLibrary.Util), "Util");
        }
    }
}
