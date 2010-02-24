using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace HeronEngine
{
    /// <summary>
    /// This class is intended to efficiently distribute work 
    /// across the number of cores. 
    /// </summary>
    public static class Parallelizer 
    {
        /// <summary>
        /// Represents a work item.
        /// </summary>
        public class Task
        {
            Action a;
            CountdownEvent countdown;

            public Task(Action a, CountdownEvent countdown)
            {
                this.a = a;
                this.countdown = countdown;
            }

            public void Execute()
            {
                a();
                countdown.Signal();
            }
        }

        /// <summary>
        /// Used to compute rudimentary profiling information
        /// </summary>
        static DateTime start = DateTime.Now;

        /// <summary>
        /// Controls whether to output profiling information or not
        /// </summary>
        static bool showTiming = true;        

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
        static Parallelizer()
        {
            Thread.CurrentThread.Name = "Main_Thread";

            int cores = Environment.ProcessorCount;
            //cores = 1;
            for (int i = 0; i < cores; ++i)
            {
                Thread t = new Thread(ThreadMain);
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

                // Note a task might still be null becaue
                // another thread might have gotten to it first
                while (task != null)
                {
                    Thread.CurrentThread.Priority = ThreadPriority.Highest;

                    PrintTime("started work");

                    // Do the work
                    task.Execute();

                    PrintTime("finished work");

                    // Get the next task
                    task = GetTask();
                }

                Thread.CurrentThread.Priority = ThreadPriority.Normal;
            }
        }

        /// <summary>
        /// Distributes work across ta number of threads equivalent to the number 
        /// of cores. All tasks will be run on the available cores. 
        /// </summary>
        /// <param name="localTasks"></param>
        public static void Invoke(params Action[] actions)
        {
            // In the degenerate case of no work, just leave
            if (actions.Length == 0)
                return;

            // Create a count-down latch that block until the count is reached
            CountdownEvent countdown = new CountdownEvent(actions.Length);

            lock (allTasks)
            {
                // Iterate over the list of localTasks, creating ta new task that 
                // will signal when it is done.
                for (int i = 0; i < actions.Length; ++i)
                {
                    // Create ta new signaling task and add it to the list
                    Task t = new Task(actions[i], countdown);
                    allTasks.Add(t);
                }
            }

            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

            // Signal to waiting threads that there is work to be done
            ReleaseThreads();

            Thread.CurrentThread.Priority = ThreadPriority.Normal;

            // Wait until all of the designated work items are completed.
            countdown.Wait();
        }

        /// <summary>
        /// Indicate to the system that the threads should terminate
        /// and unblock them.
        /// </summary>
        public static void Cleanup()
        {
            shuttingDown = true;
            ReleaseThreads();
        }
    }    
}

