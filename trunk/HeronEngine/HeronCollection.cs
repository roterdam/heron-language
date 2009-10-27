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
    public interface IHeronEnumerator
    {
        bool MoveNext(HeronVM vm);
        HeronValue GetValue(HeronVM vm);
        void Reset(HeronVM vm);
    }

    public interface IHeronEnumerable
    {
        IHeronEnumerator GetEnumerator(HeronVM vm);
    }

    public interface IHeronCollection
        : IHeronEnumerable
    {
        void Add(HeronValue v);
        int Count();
    }

    public class NewHeronCollection
        : IHeronCollection
    {
        public class HeronEnumerator
            : IHeronEnumerator
        {
            IEnumerator<HeronValue> iter;

            HeronEnumerator(IEnumerator<HeronValue> iter)
            {
                this.iter = iter;
            }

            #region IHeronEnumerator Members

            public bool MoveNext(HeronVM vm)
            {
                return iter.MoveNext();
            }

            public HeronValue GetValue(HeronVM vm)
            {
                return iter.Current;
            }

            public void Reset()
            {
                iter.Reset();
            }

            #endregion
        }

        List<HeronValue> list = new List<HeronValue>();

        public void Add(HeronValue x)
        {
            list.Add(o);
        }

        public int Count()
        {
            return list.Count;
        }

        public IHeronEnumerator GetEnumerator(HeronVM vm)
        {
            return new HeronEnumerator(list.GetEnumerator());
        }
    }
}
