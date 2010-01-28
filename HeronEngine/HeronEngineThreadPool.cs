using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace HeronEngine
{
    public delegate void Procedure();

    public static class HeronEngineThreadPool
    {
        public static void Initialize()
        {
            ThreadPool.SetMaxThreads(Config.maxThreads, 0);
        }

        public static int GetAvailableThreads()
        {
            int nWorker;
            int nIO;
            ThreadPool.GetAvailableThreads(out nWorker, out nIO);
            return nWorker;
        }

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

        public static void SplitWork(Procedure p1, Procedure p2)
        {
            SplitWork(new List<Procedure>() { p1, p2 });
        }

        public static void SplitWork(List<Procedure> procs)
        {
            MultiLock mlock = new MultiLock(procs.Count);
            for (int i = 0; i < procs.Count; ++i)
            {
                ThreadPool.QueueUserWorkItem(
                    (object o) =>
                    {
                        (o as Procedure)();
                        mlock.Release();
                    }, procs[i]);
            }
            mlock.Wait();
        }
    }
}
