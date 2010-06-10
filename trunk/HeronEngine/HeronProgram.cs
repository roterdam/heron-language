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
using System.Threading;
using System.Xml;
using System.Runtime;
using System.IO;
using System.Threading.Tasks;
using System.Deployment;
using System.Dynamic;
using System.Drawing;
using System.Windows.Forms;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Reflection.Emit;
using System.Reflection.Cache;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;
using System.Diagnostics;

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

        public ProgramDefn(string name, ModuleDefn global)
        {
            this.name = name;
            this.global = global;
        }

        public override HeronType Type
        {
            get { return PrimitiveTypes.ProgramType; }
        }
        #region heron visible functions
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

        [HeronVisible]
        public void ResolveTypes()
        {
            foreach (ModuleDefn md in GetModules())
            {
                md.ResolveTypes(this, global);
                foreach (ClassDefn c in md.GetClasses())
                    c.VerifyInterfaces();
            }
        }
    }
}
