using System;
using System.Diagnostics;
using System.Threading;

class Program
{
    const int Iterations = 50;

    static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--child")
        {
            return;
        }

        string executablePath = Environment.ProcessPath!;

        Console.WriteLine("======================================");
        Console.WriteLine(" Process vs Thread Creation Overhead");
        Console.WriteLine("======================================");

        Console.WriteLine($"Iterations: {Iterations}");
        Console.WriteLine();


        Stopwatch processStopwatch = Stopwatch.StartNew();

        for (int i = 0; i < Iterations; i++)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("--child");

            Process? childProcess = Process.Start(startInfo);

            childProcess?.WaitForExit();

            childProcess?.Dispose();
        }

        processStopwatch.Stop();

        Stopwatch threadStopwatch = Stopwatch.StartNew();

        for (int i = 0; i < Iterations; i++)
        {
            Thread thread = new Thread(() =>
            {
            });

            thread.Start();

            thread.Join();
        }

        threadStopwatch.Stop();


        double avgProcessMs =
            processStopwatch.Elapsed.TotalMilliseconds / Iterations;

        double avgThreadMs =
            threadStopwatch.Elapsed.TotalMilliseconds / Iterations;

        double ratio = avgProcessMs / avgThreadMs;

        Console.WriteLine($"Average process creation time: {avgProcessMs:F3} ms");
        Console.WriteLine($"Average thread creation time:  {avgThreadMs:F3} ms");
        Console.WriteLine($"Process/thread ratio: {ratio:F1}x");

        Console.WriteLine();

        Console.WriteLine(
            $"Process creation was {ratio:F1}x more expensive than thread creation."
        );
    }
}