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
    /// <summary>
    /// This is an association list of objects with names.
    /// This is used as a mechanism for creating scoped names.
    /// </summary>
    public class Scope : Dictionary<String, HeronValue>
    {
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string s in Keys)
            {
                sb.Append(s);
                sb.Append(" = ");
                HeronValue o = this[s];
                if (o != null)
                    sb.AppendLine(o.ToString());
                else
                    sb.AppendLine("null");
            }
            return sb.ToString();
        }
    }
}
