using System;
using System.Threading;

class Program
{
    static void Worker(object? arg)
    {
        long id = Convert.ToInt64(arg);
        Console.WriteLine($"Thread {id + 1}: is running on CPU {Thread.GetCurrentProcessorId()}");
    }

    static void Main()
    {
        int numCores = Environment.ProcessorCount;
        Console.WriteLine($"Detected logical cores: {numCores}");
        Thread[] threads = new Thread[numCores + 1];

        for (int i = 0; i < numCores + 1; i++)
        {
            int idx = i;
            threads[i] = new Thread(() => Worker(idx));
            threads[i].Start();
        }
        for (int i = 0; i < numCores + 1; i++)
        {
            threads[i].Join();
        }

        Console.WriteLine($"All {numCores} threads completed.");
    }
}