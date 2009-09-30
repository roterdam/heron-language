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
    public class NameValueTable : Dictionary<String, HeronValue>
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
