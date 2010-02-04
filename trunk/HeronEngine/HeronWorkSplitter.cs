using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace HeronEngine
{
    public delegate void Procedure();

    public class MultiLock
    {
        int count;
        EventWaitHandle h = new EventWaitHandle(false, EventResetMode.ManualReset);

        public MultiLock(int n)
        {
            if (n < 1)
                throw new ArgumentException("Require at least one item to wait on");
            count = n;
        }

        public void Wait()
        {
            h.WaitOne();
        }

        public void Release()
        {
            Interlocked.Decrement(ref count);
            if (Interlocked.Equals(count, 0))
                h.Set();
        }
    }

    public static class WorkSplitter
    {
        public static void SplitWork(List<Procedure> procs)
        {
            MultiLock mlock = new MultiLock(procs.Count);
            
            for (int i = 0; i < procs.Count; ++i)
            {
                Thread t = new Thread(
                    (object o) =>
                    {
                        (o as Procedure)();
                        mlock.Release();
                    });
                t.Priority = ThreadPriority.Highest;
                t.Start(procs[i]);
            }
            mlock.Wait();
        }        
    }
}
