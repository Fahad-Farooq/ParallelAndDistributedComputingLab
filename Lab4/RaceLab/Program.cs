using System;

namespace Progarm
{
    class Program
    {
        const int numThreads = 4;
        const int incrementPerThread = 1_000_000;
        static long counter = 0;

        static void Worker()
        {
            for (int i = 0; i < incrementPerThread; i++)
            {
                counter++;
            }
        }

        static void Main()
        {
            Thread[] threads = new Thread[numThreads];
            for (int i = 0; i < numThreads; i++)
            {
            threads[i] = new Thread(Worker);
            threads[i].Start();
            }
            for (int i = 0; i < numThreads; i++)
            {
            threads[i].Join();
            }
            long expected = (long)numThreads * incrementPerThread;
            Console.WriteLine($"Expected: {expected}");
            Console.WriteLine($"Actual: {counter}");
            Console.WriteLine($"Lost updates: {expected - counter}");
        }

    }
}