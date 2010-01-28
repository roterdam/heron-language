/// Heron language interpreter for Windows in C#
/// http://www.heron-language.com
/// Copyright (c) 2009 Christopher Diggins
/// Licenced under the MIT License 1.0 
/// http://www.opensource.org/licenses/mit-license.php

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    /// <summary>
    /// Represents an enumerator at run-time. AnyValue enumerator is also an enumerable: it just 
    /// returns itself.
    /// </summary>
    public abstract class IteratorValue
        : SeqValue
    {
        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.IteratorType;
        }

        #region iterator functions
        [HeronVisible]
        public abstract bool MoveNext();
        [HeronVisible]
        public abstract HeronValue GetValue();
        #endregion 

        #region sequence functions 
        public override IteratorValue GetIterator()
        {
            return this;
        }
        #endregion
    }

    /// <summary>
    /// An enumerator that is the result of a select operator
    /// </summary>
    public class SelectEnumerator
        : IteratorValue
    {
        IteratorValue iter;
        string name;
        Expression pred;
        HeronValue current;
        VM vm;

        public SelectEnumerator(VM vm, string name, IteratorValue iter, Expression pred)
        {
            this.vm = vm;
            this.name = name;
            this.iter = iter;
            this.pred = pred;
        }

        public override bool MoveNext()
        {
            using (vm.CreateScope())
            {
                vm.AddVar(name, Null);

                while (iter.MoveNext())
                {
                    current = iter.GetValue();
                    vm.SetVar(name, current);
                    HeronValue cond = vm.Eval(pred);
                    if (cond.ToBool())
                        return true;
                }

                current = null;
                return false;
            }
        }

        public override HeronValue GetValue()
        {
            return current;
        }
    }

    /// <summary>
    /// An enumerator that is the result of a range operator (a..b)
    /// </summary>
    public class RangeEnumerator
        : IteratorValue
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

        public override bool MoveNext()
        {
            if (next > max)
                return false;
            cur = next++;
            return true;
        }

        public override HeronValue GetValue()
        {
            return new IntValue(cur);
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.SeqType;
        }

        public override string ToString()
        {
            return min.ToString() + ".." + max.ToString();
        }
    }

    /// <summary>
    /// An enumerator that is the result of a map-each operator
    /// </summary>
    public class MapEachEnumerator
        : IteratorValue
    {
        string name;
        IteratorValue iter;
        Expression yield;
        VM vm;

        public MapEachEnumerator(VM vm, string name, IteratorValue iter, Expression yield)
        {
            this.vm = vm;
            this.name = name;
            this.iter = iter;
            this.yield = yield;
        }

        public override bool MoveNext()
        {
            return iter.MoveNext();
        }

        public override HeronValue GetValue()
        {
            using (vm.CreateScope())
            {
                vm.AddVar(name, iter.GetValue());
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
        IteratorValue iter;

        public HeronToEnumeratorAdapter(VM vm, SeqValue list)
            : this(vm, list.GetIterator())
        {
        }

        public HeronToEnumeratorAdapter(VM vm, IteratorValue iter)
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
            get { return iter.GetValue(); }
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
            get { return iter.GetValue(); }
        }

        public bool MoveNext()
        {
            return iter.MoveNext();
        }

        #endregion
    }

    /// <summary>
    /// Takes a generic .NET enumerator instance and converts it into a HeronValue
    /// specifically: an IteratorValue
    /// </summary>
    public class EnumeratorToHeronAdapter
        : IteratorValue
    {
        IEnumerator<HeronValue> iter;

        public EnumeratorToHeronAdapter(IEnumerator<HeronValue> iter)
        {
            this.iter = iter;
        }

        public override bool MoveNext()
        {
            return iter.MoveNext();
        }

        public override HeronValue GetValue()
        {
            return iter.Current;
        }
    }

    /// <summary>
    /// Represents a sequence, which is a collection which can only be
    /// iterated over once. It is constructed from a Heron enumerator
    /// </summary>
    public abstract class SeqValue
        : HeronValue
    {
        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.SeqType;
        }

        public IEnumerable<HeronValue> ToDotNetEnumerable(VM vm)
        {
            return new HeronToEnumeratorAdapter(vm, this);
        }

        public override HeronValue InvokeBinaryOperator(VM vm, string s, HeronValue x)
        {
            switch (s)
            {
                case "==":
                    return new BoolValue(EqualsValue(vm, x));
                case "!=":
                    return new BoolValue(!EqualsValue(vm, x));
                default:
                    return base.InvokeBinaryOperator(vm, s, x);
            }
        }


        public override bool EqualsValue(VM vm, HeronValue x)
        {
            if (!(x is SeqValue))
                return false;
            IteratorValue e1 = GetIterator();
            IteratorValue e2 = (x as SeqValue).GetIterator();
            bool b1 = e1.MoveNext();
            bool b2 = e2.MoveNext();

            // While both lists have data.
            while (b1 && b2)
            {
                HeronValue v1 = e1.GetValue();
                HeronValue v2 = e2.GetValue();
                if (!v1.EqualsValue(vm, v2))
                    return false;
                b1 = e1.MoveNext();
                b2 = e2.MoveNext();                
            }

            // If one of b1 or b2 is true, then we didn't get to the end of list
            // so we have different sized lists.
            if (b1 || b2) return false;

            return true;
        }
        
        [HeronVisible]
        public virtual ListValue ToList()
        {
            return new ListValue(GetIterator());
        }

        [HeronVisible]
        public abstract IteratorValue GetIterator();
    }

    /// <summary>
    /// Represents a collection which can be iterated over multiple times.
    /// </summary>
    public class ListValue
        : SeqValue
    {
        List<HeronValue> list = new List<HeronValue>();

        public ListValue()
        {
        }

        public ListValue(IEnumerable<HeronValue> xs)
        {
            list.AddRange(xs);
        }

        public ListValue(IteratorValue val)
        {
            while (val.MoveNext())
            {
                list.Add(val.GetValue());
            }
        }

        public ListValue(IList xs)
        {
            foreach (Object x in xs) 
                list.Add(DotNetObject.Marshal(x));
        }

        [HeronVisible]
        public void Add(HeronValue v)
        {
            list.Add(v);
        }

        [HeronVisible]
        public void Remove()
        {
            list.RemoveAt(list.Count - 1);
        }

        public int InternalCount()
        {
            return list.Count();
        }

        public HeronValue InternalGetAtIndex(int n)
        {
            return list[n];
        }

        [HeronVisible]
        public HeronValue Count()
        {
            return new IntValue(InternalCount());
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.ListType;
        }

        public override IteratorValue GetIterator()
        {
            return new EnumeratorToHeronAdapter(list.GetEnumerator());
        }
    
        public override ListValue ToList()
        {
            return this;
        }

        public override HeronValue GetAtIndex(HeronValue index)
        {
            if (!(index is IntValue))
                throw new Exception("Can only use index lists using integers");
            return list[(index as IntValue).GetValue()];
        }

        public override void SetAtIndex(HeronValue index, HeronValue val)
        {
            if (!(index is IntValue))
                throw new Exception("Can only use index lists using integers");
            list[(index as IntValue).GetValue()] = val;
        }

        public List<HeronValue> InternalList()
        {
            return list;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < list.Count; ++i)
            {
                if (i > Config.maxListPrintableSize)
                {
                    sb.Append("...");
                    break;
                }
                if (i > 0) sb.Append(", ");
                sb.Append(list[i].ToString());
            }
            sb.Append(']');
            return sb.ToString();
        }
    }
}
