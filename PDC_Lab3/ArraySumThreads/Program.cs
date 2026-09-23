using System;
using System.Threading;

class Program
{
    static long[] data = new long[10_000_000];

    static long[] partialSums;

    static int numWorkers;

    static void SumSlice(object? arg)
    {
        int idx = (int)arg!;

        int sliceSize = data.Length / numWorkers;

        int start = idx * sliceSize;

        int end = (idx == numWorkers - 1)
            ? data.Length
            : start + sliceSize;

        long sum = 0;

        for (int i = start; i < end; i++)
        {
            sum += data[i];
        }

        partialSums[idx] = sum;
    }

    static void Main()
    {
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = i + 1;
        }

        numWorkers = Environment.ProcessorCount;

        Console.WriteLine($"Array size: {data.Length:N0}");
        Console.WriteLine($"Worker threads: {numWorkers}");

        partialSums = new long[numWorkers];

        Thread[] threads = new Thread[numWorkers];

        for (int i = 0; i < numWorkers; i++)
        {
            int idx = i;

            threads[i] = new Thread(() => SumSlice(idx));
            threads[i].Start();
        }

        for (int i = 0; i < numWorkers; i++)
        {
            threads[i].Join();
        }

        long threadedTotal = 0;

        foreach (long partial in partialSums)
        {
            threadedTotal += partial;
        }

        long sequentialTotal = 0;

        foreach (long value in data)
        {
            sequentialTotal += value;
        }

        Console.WriteLine($"Threaded total: {threadedTotal:N0}");
        Console.WriteLine($"Sequential total: {sequentialTotal:N0}");
        Console.WriteLine($"Match: {threadedTotal == sequentialTotal}");
    }
}