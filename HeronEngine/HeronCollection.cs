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
    /* This is under development
     * 
    public interface IHeronEnumerable
    {
        IEnumerable<HeronValue> GetValues(HeronExecutor vm);
        bool GetCount(out int result);
        bool IsEmpty(out bool result);
        bool Evaluated();
    }

    public class FilteredHeronEnumerable
        : IHeronEnumerable
    {
        FunctionValue predicate;
        IHeronEnumerable list;

        FilteredHeronEnumerable(FunctionValue predicate, IHeronEnumerable list)
        {
            this.predicate = predicate;
            this.list = list;
        }

        #region IHeronEnumerable Members

        public IEnumerable<HeronValue> GetValues(HeronExecutor vm)
        {
            foreach (HeronValue v in list)
            {
                HeronValue tmp = f.Apply(vm, new HeronValue[] { v });
                if (tmp.ToBool())
                    yield return v;
            }
        }

        public bool GetCount(out int result)
        {
            result = -1;
            return false;
        }

        public bool IsEmpty(out bool result)
        {
            result = false;
            return false;
        }

        public bool Evaluated()
        {
            return false;
        }

        #endregion
    }

    public static class IHeronEnumerableExtensions
    {
        public IHeronEnumerable Select(this IHeronEnumerable self, FunctionValue f)
        {
            
        }

        IHeronEnumerable ForEach(FunctionValue f);
        HeronValue Accumulate(HeronValue init, FunctionValue f);
    }

    public class IntRange : IHeronEnumerable   
    {
        int min;
        int max;

        public IntRange(int min, int max)
        {
        }
    }
    */

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
