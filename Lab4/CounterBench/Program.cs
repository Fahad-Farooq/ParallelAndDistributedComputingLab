using System;
using System.Diagnostics;
using System.Threading;

class Program
{
    const int NumThreads = 4;
    const int IncrementsPerThread = 1_000_000;

    static long counter = 0;

    static readonly object counterLock = new object();

    // Version 1: Critical section using lock
    static void LockWorker()
    {
        for (int i = 0; i < IncrementsPerThread; i++)
        {
            lock (counterLock)
            {
                counter++;
            }
        }
    }

    // Version 2: Atomic increment using Interlocked
    static void InterlockedWorker()
    {
        for (int i = 0; i < IncrementsPerThread; i++)
        {
            Interlocked.Increment(ref counter);
        }
    }

    static double RunVersion(string name, ThreadStart worker)
    {
        // Reset shared counter
        counter = 0;

        Thread[] threads = new Thread[NumThreads];

        // Start stopwatch before creating/starting the worker threads
        Stopwatch sw = Stopwatch.StartNew();

        // Create and start all threads
        for (int i = 0; i < NumThreads; i++)
        {
            threads[i] = new Thread(worker);
            threads[i].Start();
        }

        // Wait for all threads to finish
        for (int i = 0; i < NumThreads; i++)
        {
            threads[i].Join();
        }

        sw.Stop();

        long expected = (long)NumThreads * IncrementsPerThread;
        double ms = sw.Elapsed.TotalMilliseconds;

        Console.WriteLine(
            $"[{name}] expected={expected} actual={counter} " +
            $"correct={counter == expected} time={ms:F2} ms"
        );

        return ms;
    }

    static void Main()
    {
        // Warm-up pass so the JIT compiles both workers
        RunVersion("warm-up lock", LockWorker);
        RunVersion("warm-up Interlocked", InterlockedWorker);

        // Benchmark
        RunVersion("lock", LockWorker);
        RunVersion("Interlocked.Increment", InterlockedWorker);
    }
}
