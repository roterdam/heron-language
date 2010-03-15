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
using System.Threading.Tasks;

namespace HeronEngine
{
    public interface IInternalIndexable
    {
        int InternalCount();
        HeronValue InternalAt(int n);
    }

    public struct LoopParams
    {
        public OptimizationParams op;
        public VM vm;
        public Accessor acc;
        public Accessor acc2;
        public Expression expr;
    }

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
        [HeronVisible]
        public abstract IteratorValue Restart();
        #endregion 

        #region sequence functions 
        public override IteratorValue GetIterator()
        {
            return Restart();
        }
        #endregion
    }

    /// <summary>
    /// An enumerator that is the result of a range operator (a..b)
    /// </summary>
    public class RangeEnumerator
        : IteratorValue, IInternalIndexable
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

        public override IteratorValue Restart()
        {
            return new RangeEnumerator(new IntValue(min), new IntValue(max));
        }

        public override ListValue ToList()
        {
            return new ListValue((IList)ToArray());
        }

        public override HeronValue[] ToArray()
        {
            int cnt = max - min + 1;
            HeronValue[] r = new HeronValue[cnt];
            for (int i = 0; i < cnt; ++i)
                r[i] = new IntValue(min + i);
            return r;
        }

        public override IInternalIndexable GetIndexable()
        {
            return this;
        }

        public int InternalCount()
        {
            return max - min + 1;
        }

        public HeronValue InternalAt(int n)
        {
            return new IntValue(min + n);
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

        public override bool Equals(Object x)
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
                if (!v1.Equals(v2))
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

        public abstract HeronValue[] ToArray();

        [HeronVisible]
        public abstract IteratorValue GetIterator();

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public abstract IInternalIndexable GetIndexable();
    }
   
    /// <summary>
    /// Represents a collection which can be iterated over multiple times.
    /// </summary>
    public class ListValue
        : SeqValue, IInternalIndexable
    {
        List<HeronValue> list = new List<HeronValue>();

        public ListValue()
        {
        }

        public ListValue(IEnumerable<HeronValue> xs)
        {
            list.AddRange(xs);
        }

        public ListValue(List<HeronValue> xs)
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
        public HeronValue Slice(IntValue from, IntValue cnt)
        {
            int nCnt = cnt.GetValue();
            int nFrom = from.GetValue();
            List<HeronValue> r = new List<HeronValue>();
            for (int i=0; i < nCnt; ++i)
            {
                r.Add(list[i + nFrom]);
            }
            return new ListValue(r);
        }

        [HeronVisible]
        public void Add(HeronValue v)
        {
            list.Add(v);
        }

        [HeronVisible]
        public void AddRange(HeronValue v)
        {
            SeqValue sv = v as SeqValue;
            if (sv == null)
                throw new Exception("Can only add a list value as a range");
            list.AddRange(sv.ToList().InternalList());
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

        public HeronValue InternalAt(int n)
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
            return new ListToIterValue(list);
        }
    
        public override ListValue ToList()
        {
            return this;
        }

        public override HeronValue GetAtIndex(HeronValue index)
        {
            IntValue iv = index as IntValue;
            if (iv == null)
                throw new Exception("Can only use index lists using integers");
            return list[iv.GetValue()];
        }

        public override void SetAtIndex(HeronValue index, HeronValue val)
        {
            IntValue iv = index as IntValue;
            if (iv == null)
                throw new Exception("Can only use index lists using integers");
            list[iv.GetValue()] = val;
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

        public override HeronValue[] ToArray()
        {
            return list.ToArray();
        }

        public override IInternalIndexable GetIndexable()
        {
            return this;
        }
    }

    /// <summary>
    /// Represents a collection which can be iterated over multiple times.
    /// </summary>
    public class ArrayValue
        : SeqValue, IInternalIndexable
    {
        HeronValue[] array;

        public ArrayValue(HeronValue[] xs)
        {
            array = xs;
        }

        [HeronVisible]
        public HeronValue Count()
        {
            return new IntValue(array.Length);
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.ArrayType;
        }

        public override IteratorValue GetIterator()
        {
            return new ListToIterValue(array);
        }

        public override ListValue ToList()
        {
            return new ListValue(new List<HeronValue>(array));
        }

        public override HeronValue GetAtIndex(HeronValue index)
        {
            IntValue iv = index as IntValue;
            if (iv == null)
                throw new Exception("Can only index an array using integers");
            return array[iv.GetValue()];
        }

        public override void SetAtIndex(HeronValue index, HeronValue val)
        {
            IntValue iv = index as IntValue;
            if (iv == null)
                throw new Exception("Can only index an array using integers");
            array[(index as IntValue).GetValue()] = val;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < array.Length; ++i)
            {
                if (i > Config.maxListPrintableSize)
                {
                    sb.Append("...");
                    break;
                }
                if (i > 0) sb.Append(", ");
                sb.Append(array[i].ToString());
            }
            sb.Append(']');
            return sb.ToString();
        }

        public override HeronValue[] ToArray()
        {
            return array;
        }

        #region IInternalIndexable Members

        public int InternalCount()
        {
            return array.Length;
        }

        public HeronValue InternalAt(int n)
        {
            return array[n];
        }

        #endregion

        public override IInternalIndexable GetIndexable()
        {
            return this;
        }
    }

    public class SliceValue : SeqValue, IInternalIndexable
    {
        ListValue list;
        int from;
        int cnt;

        public SliceValue(ListValue list, int from, int cnt)
        {
            this.list = list;
            this.from = from;
            this.cnt = cnt;
        }

        public override HeronValue[] ToArray()
        {
            HeronValue[] a = new HeronValue[InternalCount()];
            for (int i = 0; i < cnt ; ++i)
                a[i + from] = list.InternalAt(i);
            return a;
        }

        public IEnumerable<HeronValue> GetEnumerable()
        {
            return ToArray();
        }

        public override IteratorValue GetIterator()
        {
            return new ListToIterValue(GetEnumerable());
        }

        public override ListValue ToList()
        {
            return new ListValue(GetEnumerable());
        }

        public override IInternalIndexable GetIndexable()
        {
            return this;
        }

        #region IInternalIndexable Members

        public int InternalCount()
        {
            return cnt;
        }

        public HeronValue InternalAt(int n)
        {
            return list.InternalAt(from + n);
        }

        [HeronVisible]
        public HeronValue Count()
        {
            return new IntValue(InternalCount());
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.SliceType;
        }

        #endregion

        public override HeronValue GetAtIndex(HeronValue index)
        {
            IntValue iv = index as IntValue;
            if (iv == null)
                throw new Exception("Can only index slices using integers");
            return list.GetAtIndex(new IntValue(iv.GetValue() + from)); 
        }

        public override void SetAtIndex(HeronValue index, HeronValue val)
        {
            IntValue iv = index as IntValue;
            if (iv == null)
                throw new Exception("Can only index slices using integers");
            list.SetAtIndex(new IntValue(iv.GetValue() + from), val);
        }
    }
}
