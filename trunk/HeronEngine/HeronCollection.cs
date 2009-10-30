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
    }

    public abstract class EnumeratorValue
        : HeronValue, IHeronEnumerator
    {
        public override HeronType GetHeronType()
        {
            return HeronPrimitiveTypes.IteratorType;
        }
        public abstract bool MoveNext(HeronVM vm);
        public abstract HeronValue GetValue(HeronVM vm);
    }

    public class SelectEnumerator
        : EnumeratorValue
    {
        IHeronEnumerator iter;
        string name;
        Expression pred;
        HeronValue current;

        public SelectEnumerator(HeronVM vm, string name, IHeronEnumerator iter, Expression pred)
        {
            this.name = name;
            this.iter = iter;
            this.pred = pred;
        }

        public override bool MoveNext(HeronVM vm)
        {
            using (vm.CreateScope())
            {
                vm.AddVar(name, null);

                while (iter.MoveNext(vm))
                {
                    current = iter.GetValue(vm);
                    vm.SetVar(name, current);
                    HeronValue cond = vm.Eval(pred);
                    if (cond.ToBool())
                        return true;
                }

                current = null;
                return false;
            }
        }

        public override HeronValue GetValue(HeronVM vm)
        {
            return current;
        }
    }

    public class RangeEnumerator
        : EnumeratorValue
    {
        int min;
        int max;
        int cur;
        int next;

        public RangeEnumerator(IntValue min, IntValue max)
        {
            this.min = min.GetValue();
            this.max = max.GetValue();
            cur = this.min;
            next = this.min;
        }

        public override bool MoveNext(HeronVM vm)
        {
            if (next > max)
                return false;
            cur = next++;
            return true;
        }

        public override HeronValue GetValue(HeronVM vm)
        {
            return new IntValue(cur);
        }

        public override HeronType GetHeronType()
        {
            return HeronPrimitiveTypes.SeqType;
        }
    }

    public class MapEachEnumerator
        : EnumeratorValue
    {
        string name;
        IHeronEnumerator iter;
        Expression yield;

        public MapEachEnumerator(string name, IHeronEnumerator iter, Expression yield)
        {
            this.name = name;
            this.iter = iter;
            this.yield = yield;
        }

        public override bool MoveNext(HeronVM vm)
        {
            return iter.MoveNext(vm);
        }

        public override HeronValue GetValue(HeronVM vm)
        {
            using (vm.CreateScope())
            {
                vm.AddVar(name, iter.GetValue(vm));
                return vm.Eval(yield);
            }
        }
   }

    public class HeronToEnumeratorAdapter
        : IEnumerable<HeronValue>, IEnumerator<HeronValue>
    {
        HeronVM vm;
        IHeronEnumerator iter;

        public HeronToEnumeratorAdapter(HeronVM vm, IHeronEnumerable list)
            : this(vm, list.GetEnumerator(vm))
        {
        }

        public HeronToEnumeratorAdapter(HeronVM vm, IHeronEnumerator iter)
        {
            this.vm = vm;
            this.iter = iter;
        }

        #region IEnumerable<HeronValue> Members

        public IEnumerator<HeronValue> GetEnumerator()
        {
            return this;
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this; ;
        }

        #endregion

        #region IEnumerator<HeronValue> Members

        public HeronValue Current
        {
            get { return iter.GetValue(vm); }
        }

        public void Reset()
        {
            // This is allowed according to MSDN
            throw new NotImplementedException();
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion

        #region IEnumerator Members

        object System.Collections.IEnumerator.Current
        {
            get { return iter.GetValue(vm); }
        }

        public bool MoveNext()
        {
            return iter.MoveNext(vm);
        }

        #endregion
    }

    public interface IHeronEnumerable
    {
        IHeronEnumerator GetEnumerator(HeronVM vm);
    }

    /// <summary>
    /// Represents a sequence, which is a collection which can only be
    /// iterated over once 
    /// </summary>
    public abstract class SeqValue
        : HeronValue, IHeronEnumerable
    {
        public abstract IHeronEnumerator GetEnumerator(HeronVM vm);

        public override HeronType GetHeronType()
        {
            return HeronPrimitiveTypes.SeqType;
        }
    }

    public class EnumeratorToHeronAdapter
        : EnumeratorValue
    {
        IEnumerator<HeronValue> iter;

        public EnumeratorToHeronAdapter(IEnumerator<HeronValue> iter)
        {
            this.iter = iter;
        }

        public override bool MoveNext(HeronVM vm)
        {
            return iter.MoveNext();
        }

        public override HeronValue GetValue(HeronVM vm)
        {
            return iter.Current;
        }
    }

    /// <summary>
    /// Represents a collection which can be iterated over multiple times.
    /// </summary>
    public class ListValue
        : SeqValue, IHeronCollection
    {
        List<HeronValue> list = new List<HeronValue>();

        public override IHeronEnumerator GetEnumerator(HeronVM vm)
        {
            return new EnumeratorToHeronAdapter(list.GetEnumerator());    
        }

        public void Add(HeronValue v)
        {
            list.Add(v);
        }

        public int Count()
        {
            return list.Count();
        }
    }

    public interface IHeronCollection
        : IHeronEnumerable
    {
        void Add(HeronValue v);
        int Count();
    }

    /* TODO: remove
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
            #endregion
        }

        List<HeronValue> list = new List<HeronValue>();

        public void Add(HeronValue x)
        {
            list.Add(x);
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
     */
}
