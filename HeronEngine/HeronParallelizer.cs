using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace HeronEngine
{
    /// <summary>
    /// Represents a work item.
    /// </summary>
    public delegate void Task();

    /// <summary>
    /// Represents a work item for a chunk of data.
    /// </summary>
    public delegate void ArrayTask<T>(T[] xs);

    /// <summary>
    /// This class is intended to efficiently distribute work 
    /// across the number of cores. 
    /// </summary>
    public static class Parallelizer 
    {
        static DateTime start;
        static bool showTiming = true;

        /// <summary>
        /// Releases ta thread when ta count reaches zero
        /// http://msdn.microsoft.com/en-us/magazine/cc163427.aspx#S1
        /// </summary>
        public class CountDownLatch
        {
            private int count;
            private EventWaitHandle handle = new ManualResetEvent(false);

            public CountDownLatch(int count)
            {
                this.count = count;
            }

            public void Decrement()
            {
                if (Interlocked.Decrement(ref count) == 0)
                    handle.Set();
            }

            public void Wait()
            {
                handle.WaitOne();
            }
        }
        
        /// <summary>
        /// List of tasks that haven't been yet acquired by ta thread 
        /// </summary>
        static List<Task> allTasks = new List<Task>();
        
        /// <summary>
        /// List of threads. Should be one per core. 
        /// </summary>
        static List<Thread> threads = new List<Thread>();
      
        /// <summary>
        /// When set signals that there is more work to be done
        /// </summary>
        static ManualResetEvent signal = new ManualResetEvent(false);

        /// <summary>
        /// Used to tell threads to stop working.
        /// </summary>
        static bool shuttingDown = false;

        /// <summary>
        /// Creates ta number of high-priority threads for performing 
        /// work. The hope is that the OS will assign each thread to 
        /// ta separate core.
        /// </summary>
        /// <param name="cores"></param>
        public static void Initialize(int cores)
        {
            start = DateTime.Now;
            Thread.CurrentThread.Name = "Main_Thread";

            for (int i = 0; i < cores; ++i)
            {
                Thread t = new Thread(ThreadMain);
                // This system is not designed to play well with others
                t.Priority = ThreadPriority.Highest;
                t.Name = "Thread_" + i.ToString();
                threads.Add(t);
                t.Start();
            }
        }

        /// <summary>
        /// Indicates to all threads that there is work
        /// to be done.
        /// </summary>
        public static void ReleaseThreads()
        {
            signal.Set();
        }

        /// <summary>
        /// Used to indicate that there is no more work 
        /// to be done, by unsetting the signal. Note: 
        /// will not work if shutting down.
        /// </summary>
        public static void BlockThreads()
        {
            if (!shuttingDown)
                signal.Reset();
        }

        /// <summary>
        /// Returns any tasks queued up to perform, 
        /// or NULL if there is no work. It will reset
        /// the global signal effectively blocking all threads
        /// if there is no more work to be done.
        /// </summary>
        /// <returns></returns>
        public static Task GetTask()
        {
            lock (allTasks)
            {
                if (allTasks.Count == 0)
                {
                    BlockThreads();
                    return null;
                }

                Task t = allTasks[allTasks.Count - 1];
                allTasks.RemoveAt(allTasks.Count - 1);
                return t;
            }
        }

        /// <summary>
        /// A rudimentary profiling tool for identifying when task execution starts and finishes.
        /// </summary>
        /// <param name="msg"></param>
        private static void PrintTime(string msg)
        {
            if (showTiming)
            {
                TimeSpan ts = DateTime.Now - start;
                Console.WriteLine(Thread.CurrentThread.Name + " " + msg + " at " + ts.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Primary function for each thread
        /// </summary>
        public static void ThreadMain()
        {
            while (!shuttingDown)
            {
                // Wait until work is available
                signal.WaitOne();

                // Get an available task
                Task task = GetTask();

                // Note ta task might still be null becaue
                // another thread might have gotten to it first
                while (task != null)
                {
                    PrintTime("started work");

                    // Do the work
                    task();

                    PrintTime("finished work");

                    // Get the next task
                    task = GetTask();
                }
            }
        }

        /// <summary>
        /// Distributes work across ta number of threads equivalent to the number 
        /// of cores. All tasks will be run on the available cores. 
        /// </summary>
        /// <param name="localTasks"></param>
        public static void DistributeWork(List<Task> localTasks)
        {
            // In the degenerate case of no work, just leave
            if (localTasks.Count == 0)
            {
                return;
            }

            // If there is only one task, just execute it.
            if (localTasks.Count == 1)
            {
                PrintTime("started work");
                localTasks[0]();
                PrintTime("finished work");
                return;
            }

            // Create ta count-down latch that block until ta count is decremented to zero
            CountDownLatch latch = new CountDownLatch(localTasks.Count);

            lock (allTasks)
            {
                // Iterate over the list of localTasks, creating ta new task that 
                // will signal when it is done.
                for (int i = 0; i < localTasks.Count; ++i)
                {
                    Task t = localTasks[i];

                    // Create an event used to signal that the task is complete
                    ManualResetEvent e = new ManualResetEvent(false);

                    // Create ta new signaling task and add it to the list
                    Task signalingTask = () => { t(); latch.Decrement(); };
                    allTasks.Add(signalingTask);
                }
            }

            // Signal to waiting threads that there is work
            ReleaseThreads();

            // Wait until all of the designated work items are completed.
            latch.Wait();
        }

        /// <summary>
        /// Indicate to the system that the threads should terminate
        /// and unblock them.
        /// </summary>
        public static void CleanUp()
        {
            shuttingDown = true;
            ReleaseThreads();
        }
    }    
}

