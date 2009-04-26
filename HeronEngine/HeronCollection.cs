using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    public class HeronCollection
    {
        List<Object> list = new List<Object>();

        public void Add(Object o)
        {
            list.Add(o);
        }

        public int Count()
        {
            return list.Count;
        }

        internal IEnumerable<Object> InternalGetList()
        {
            return list;
        }
    }
}
