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
        IEnumerable<HeronValue> GetValues(HeronVM vm);
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

        public IEnumerable<HeronValue> GetValues(HeronVM vm)
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

    interface HIIterator
    {
        bool MoveToNext();
        bool Reset();
        HeronValue GetValue();
        HeronType GetValueType();
    }

    interface HIBiDirIterator
        : HIIterator
    {
        bool MoveToPrevious();
    }

    interface HIIterable
    {
        HIIterator GetIterator();
    }

    interface HICollection
        : HIIterator
    {
        HeronType GetValueType();
        HIIterator GetIterator();
    }

    interface HIUnordered
        : HICollection
    {
        bool Exists(HeronValue x);
        void Insert(HeronValue x);
        bool Remove(HeronValue x);
    }

    interface HIStack
        : HICollection
    {
        void Push(HeronValue x);
        void Pop();
        HeronValue Peek();
    }

    interface HIQueue
    {
        void Enqueue(HeronValue x);
        HeronValue Dequeue();
        void Clear();
        bool IsEmpty();
    }

    interface HIDeque
    {
        void EnqueueFront(HeronValue x);
        HeronValue DequeueFront();
        void EnqueueBack(HeronValue x);
        HeronValue DequeueBack();
        void Clear();
        bool IsEmpty();
        bool IsFull();
    }

    interface HIIndexable
        : HICollection
    {
        bool Exists(HeronValue index);
        HeronValue GetValueAt(HeronValue index);
        void SetValueAt(HeronValue index, HeronValue value);
    }

    interface HISource
    {
        HeronValue Read();
        bool HasData();
        void WaitForData();
        bool IsOpen();            
    }

    interface HISink
    {
        bool Write(HeronValue x);
    }

    class CollectionToSource
        : IteratorToSource
    {
        public CollectionToSource(HICollection c)
        {
            base(c.GetIter());
        }
    }

    class IteratorToSource : HISource
    {
        HIIterator iter;
        bool hasData;

        public IteratorToSource(HIIterator iter)
        {
            this.iter = iter;
            iter.Reset();
            hasData = iter.MoveToNext();
        }

        #region HISource Members

        public HeronValue Read()
        {
            if (!hasData)
                throw new Exception("Source closed");
            return iter.GetValue();
            hasData = iter.MoveNext;
        }

        public bool HasData()
        {
            return hasData;
        }

        public void WaitForData()
        {
            throw new Exception("No data available");
        }

        public bool IsOpen()
        {
            return hasData;
        }

        #endregion
    }

    interface HIFilter
    {
        void Start();
        void SetStdIn(HISource stream);
        void SetStdOut(HISink stream);
        HISource GetStdIn();
        HISink GetStdOut();
        bool IsFinished();
    }

    interface HIProgram : HIFilter
    {
        string Name { get; }
    }

    interface HIClosure : HIIterator
    {
        string Name { get; }
        string[] ArgNames { get; }
        HeronType[] ArgTypes { get; }
        void Create(HeronValue[] args);
    }

    /// <summary>
    /// O(1) lookup when exists 
    /// O(LogN) lookup when doesn't exist
    /// 
    /// When increasing size, copy all of the old data into the new data structure. 
    /// This speeds up lookup. Leave the old data. There is going to be an increas of size each time.
    /// Removing becomes tricky. I have to remove it from everywhere. 
    /// Probably only a couple of times. Sol'n, keep a count of it.
    /// (N + 1/2 N + 1/4N) + (1/2 N + 1/4 N) + (1/4 N)
    /// 1x 4x 16x 32x => This growth is too fast, I think? 
    /// Looks like N*log2N size.
    /// What if items moved? 
    /// That would invalidate pointers. Which isn't terribly cool. 
    /// The persistent memory aspect seems to excite a bunch of people. For me it gives me a pointer
    /// that isn't invalidated. I guess it means though that in theory different people can share the same structure, and
    /// not have to update their versions.
    /// So does it mean there needs to be a "VTable" view? This way if some modifies a VTable, they get a new one?
    /// Or whenever someone calls "Remove" they automatically get the new one? Well that could be done,
    /// but if there is no sharing it is terribly inefficient to do that.
    /// However, how often do we need "immutable" hash tables? Well I guess the theory is that if a table can't 
    /// be modified, then it can be updated by multiple threads efficient. That is kind of cool.
    /// Now that I think about it, this is kind of cool. 
    /// 
    /// So I would have a table that is inefficient on a single thread, but that can handle lots a
    /// </summary>
    /// <typeparam name="indexT"></typeparam>
    /// <typeparam name="valueT"></typeparam>
    public class HeronVTable<indexT, valueT>
    {
        class Buffer
        {
            Dictionary<indexT, valueT> d = new Dictionary<indexT, valueT>();
            int max;
        }

        void Test()
        {
            List<valueT> test;
        }
    }

    public class Stack<T>
    {
        StackImpl impl;
        int index;
        
        public class StackImpl
        {
            int refCnt;

            StackBuffer root;
            StackBuffer cur;

            public class StackBuffer
            {
                StackBuffer prev;
                T[] data;
                int cur;
            }
        }

        public void Push(T x)
        {
            if (data.refCnt != 1)
            {
                // if we are sharing the impl class
                // we are going to have to clone it.
            }
        }

        public void Pop()
        {
            --index;
            if (impl.refCnt == 0)
                impl.refCnt == 1;
        }

        public void Copy()
        {
        }
    }
    /* This is under development
     * 
    public interface IHeronEnumerable
    {
        IEnumerable<HeronValue> GetValues(HeronVM vm);
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

        public IEnumerable<HeronValue> GetValues(HeronVM vm)
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

}
