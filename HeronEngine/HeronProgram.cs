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
using System.Text.RegularExpressions;
using System.IO;

namespace HeronEngine
{
    /// <summary>
    /// Represents an executable Heron program.
    /// </summary>
    public class ProgramDefn : HeronValue
    {
        private List<ModuleDefn> modules = new List<ModuleDefn>();
        private Dictionary<string, ModuleDefn> dependencies = new Dictionary<string, ModuleDefn>();
        private ModuleDefn global;

        [HeronVisible]
        public string name;

        public ProgramDefn(string name)
        {
            this.name = name;
            global = new ModuleDefn(this, "_global_");
            RegisterPrimitives();
        }
        public void RegisterDotNetType(Type t, string name)
        {
            global.AddDotNetType(name, t);
        }
        public void RegisterDotNetType(Type t)
        {
            global.AddDotNetType(t.Name, t);
        }
        /// <summary>
        /// This exposes a set of globally recognized Heron and .NET 
        /// types to the environment (essentially global variables).
        /// A simple way to extend the scope of Heron is to introduce
        /// new types in this function.
        /// </summary>
        private void RegisterPrimitives()
        {
            SortedDictionary<string, HeronType> prims = PrimitiveTypes.GetTypes();
            foreach (string s in prims.Keys)
                global.AddPrimitive(s, prims[s]);

            RegisterDotNetType(typeof(Console));
            RegisterDotNetType(typeof(Math));
            RegisterDotNetType(typeof(File));
            RegisterDotNetType(typeof(Directory));
            RegisterDotNetType(typeof(Regex));
            
            

            // Load other libraries specified in the configuration file
            foreach (string lib in Config.libs)
                LoadAssembly(lib);

            // Load the standard library types
            RegisterDotNetType(typeof(HeronStandardLibrary.Viewport), "Viewport");
            RegisterDotNetType(typeof(HeronStandardLibrary.Util), "Util");
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.ProgramType;
        }
        #region heron visible functions
        [HeronVisible]
        public void LoadAssembly(string s)
        {
            Assembly a = null;
            foreach (String tmp in Config.inputPath)
            {
                string path = tmp + "\\" + s;
                if (File.Exists(path))
                {
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
            LoadAssembly(a);
        }

        public void LoadAssembly(Assembly a)
        {
            foreach (Type t in a.GetExportedTypes())
                RegisterDotNetType(t);
        }

        /// <summary>
        /// Adds the module, and tracks dependencies on other modules.
        /// </summary>
        /// <param name="mi">The new module being loaded, must not already be loaded</param>
        [HeronVisible]
        public void AddModule(ModuleDefn m)
        {
            SatisfyOpenDependencies(m);
            AddNewDependencies(m);
            modules.Add(m);
        }

        /// <summary>
        /// Closes open dendencies
        /// </summary>
        /// <param name="mi"></param>
        public void SatisfyOpenDependencies(ModuleDefn m)
        {
            if (dependencies.ContainsKey(m.name))
            {
                if (dependencies[m.name] != null)
                {
                    throw new Exception(m.name + " is already parsed and loaded");
                }
                else
                {
                    dependencies[m.name] = m;
                }
            }
            else
            {
                dependencies.Add(m.name, m);
            }
        }

        /// <summary>
        /// Adds new dependencies, based on imported modules
        /// </summary>
        /// <param name="mi"></param>
        public void AddNewDependencies(ModuleDefn m)
        {
            foreach (string s in m.GetImportedModuleNames())
            {
                if (!dependencies.ContainsKey(s))
                    dependencies.Add(s, null);
            }

            // There may also be an imported class
            if (m.HasBaseClass())
            {
                string s = m.GetInheritedClassName();
                if (!dependencies.ContainsKey(s))
                    dependencies.Add(s, null);
            }
        }

        /// <summary>
        /// Returns a list of names of modules that need to be imported
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetUnloadedDependentModules()
        {
            foreach (string s in dependencies.Keys)
                if (dependencies[s] == null)
                    yield return s;
        }

        [HeronVisible]
        public ModuleDefn GetGlobal()
        {
            return global;
        }

        [HeronVisible]
        public IEnumerable<ModuleDefn> GetModules()
        {
            return modules;
        }
        
        [HeronVisible]
        public ModuleDefn GetModule(string s)
        {
            return dependencies[s];
        }
        #endregion
    }
}
