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
        bool MoveNext(VM vm);
        HeronValue GetValue(VM vm);
    }

    public abstract class EnumeratorValue
        : HeronValue, IHeronEnumerator
    {
        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.IteratorType;
        }
        public abstract bool MoveNext(VM vm);
        public abstract HeronValue GetValue(VM vm);
    }

    public class SelectEnumerator
        : EnumeratorValue
    {
        IHeronEnumerator iter;
        string name;
        Expression pred;
        HeronValue current;

        public SelectEnumerator(VM vm, string name, IHeronEnumerator iter, Expression pred)
        {
            this.name = name;
            this.iter = iter;
            this.pred = pred;
        }

        public override bool MoveNext(VM vm)
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

        public override HeronValue GetValue(VM vm)
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

        public override bool MoveNext(VM vm)
        {
            if (next > max)
                return false;
            cur = next++;
            return true;
        }

        public override HeronValue GetValue(VM vm)
        {
            return new IntValue(cur);
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.SeqType;
        }
    }

    /// <summary>
    /// Created by mapeach statements when used on an IHeronEnumerator 
    /// </summary>
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

        public override bool MoveNext(VM vm)
        {
            return iter.MoveNext(vm);
        }

        public override HeronValue GetValue(VM vm)
        {
            using (vm.CreateScope())
            {
                vm.AddVar(name, iter.GetValue(vm));
                return vm.Eval(yield);
            }
        }
   }

    /// <summary>
    /// Used by IHeronEnumerableExtension to convert any IHeronEnumerable into a 
    /// an IEnumerable, so that we can use "foreach" statements
    /// </summary>
    public class HeronToEnumeratorAdapter
        : IEnumerable<HeronValue>, IEnumerator<HeronValue>
    {
        VM vm;
        IHeronEnumerator iter;

        public HeronToEnumeratorAdapter(VM vm, IHeronEnumerable list)
            : this(vm, list.GetEnumerator(vm))
        {
        }

        public HeronToEnumeratorAdapter(VM vm, IHeronEnumerator iter)
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

    /// <summary>
    /// Simple interfaces for anything which can be enumerated (collections, streams, ranges, etc.)
    /// </summary>
    public interface IHeronEnumerable
    {
        IHeronEnumerator GetEnumerator(VM vm);
    }

    /// <summary>
    /// Extends IHeronEnumerable instances with conversion functions
    /// </summary>
    public static class IHeronEnumerableExtension
    {
        public static IEnumerable<HeronValue> ToDotNetEnumerable(this IHeronEnumerable self, VM vm)
        {
            return new HeronToEnumeratorAdapter(vm, self);
        }
        public static ListValue ToList(this IHeronEnumerable self, VM vm)
        {
            if (self is ListValue)
                return self as ListValue;
            return new ListValue(self.ToDotNetEnumerable(vm));
        }
    }

    /// <summary>
    /// Represents a sequence, which is a collection which can only be
    /// iterated over once 
    /// </summary>
    public abstract class SeqValue
        : HeronValue, IHeronEnumerable
    {
        public abstract IHeronEnumerator GetEnumerator(VM vm);

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.SeqType;
        }
    }

    /// <summary>
    /// Takes an IEnumerator instance and converts it into a HeronValue
    /// specifically: an EnumeratorValue
    /// </summary>
    public class EnumeratorToHeronAdapter
        : EnumeratorValue
    {
        IEnumerator<HeronValue> iter;

        public EnumeratorToHeronAdapter(IEnumerator<HeronValue> iter)
        {
            this.iter = iter;
        }

        public override bool MoveNext(VM vm)
        {
            return iter.MoveNext();
        }

        public override HeronValue GetValue(VM vm)
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

        public ListValue()
        {
        }

        public ListValue(IEnumerable<HeronValue> xs)
        {
            list.AddRange(xs);
        }

        public override IHeronEnumerator GetEnumerator(VM vm)
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
}
