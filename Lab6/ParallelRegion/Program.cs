
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        int maxDegree = args.Length > 0
            ? int.Parse(args[0])
            : -1;

        Console.WriteLine($"Environment.ProcessorCount: {Environment.ProcessorCount}");
        Console.WriteLine($"Maximum degree of parallelism: " +
            (maxDegree == -1 ? "Default" : maxDegree));
        Console.WriteLine();

        const int iterations = 20;

        var threadIds = new ConcurrentDictionary<int, byte>();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegree
        };

        Stopwatch stopwatch = Stopwatch.StartNew();

        Parallel.For(0, iterations, options, i =>
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;

            threadIds.TryAdd(threadId, 0);

            Console.WriteLine(
                $"Iteration {i}: Thread ID = {threadId}");

            Thread.Sleep(100);
        });

        stopwatch.Stop();

        int predictedTime = (int)Math.Ceiling(
            (double)iterations / (maxDegree == -1
                ? Environment.ProcessorCount
                : maxDegree)) * 100;

        Console.WriteLine();
        Console.WriteLine("========== Results ==========");
        Console.WriteLine($"Total iterations : {iterations}");
        Console.WriteLine($"Distinct threads : {threadIds.Count}");
        Console.WriteLine($"Thread IDs seen  : {string.Join(", ", threadIds.Keys.OrderBy(id => id))}");
        Console.WriteLine($"Elapsed time     : {stopwatch.Elapsed.TotalMilliseconds:F2} ms");
        Console.WriteLine($"Predicted time   : {predictedTime} ms");
    }
}