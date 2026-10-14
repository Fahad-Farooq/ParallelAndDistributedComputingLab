using System;
using System.Diagnostics;
using System.Threading;

class Program
{
    static readonly object Locker = new object();

    static bool dataReady = false;
    static int value = 0;

    static int timesBlocked = 0;

    static void Worker()
    {
        lock (Locker)
        {
            // Wait until the predicate (dataReady) becomes true.
            while (!dataReady)
            {
                timesBlocked++;
                Monitor.Wait(Locker);
            }

            Console.WriteLine($"Worker received value: {value}");
        }
    }

    static void Publish(int newValue)
    {
        lock (Locker)
        {
            value = newValue;
            dataReady = true;

            // Wake one waiting worker.
            Monitor.Pulse(Locker);
        }
    }

    static void RunConditionScenario(int workers, bool signalFirst)
{
    dataReady = false;
    value = 0;
    timesBlocked = 0;

    Thread[] threads = new Thread[workers];

    Stopwatch wall = Stopwatch.StartNew();
    Stopwatch cpu = Stopwatch.StartNew();

    if (signalFirst)
    {
        // Publish before starting the worker.
        Publish(42);
    }

    for (int i = 0; i < workers; i++)
    {
        threads[i] = new Thread(Worker);
        threads[i].Start();
    }

    if (!signalFirst)
    {
        // Allow workers to enter Monitor.Wait().
        Thread.Sleep(100);

        Publish(42);
    }

    // For 3-worker scenario, Pulse() wakes only one worker.
    // Therefore, use PulseAll() so all workers can finish.
    if (workers == 3)
    {
        lock (Locker)
        {
            Monitor.PulseAll(Locker);
        }
    }

    foreach (Thread t in threads)
    {
        t.Join();
    }

    wall.Stop();
    cpu.Stop();

    Console.WriteLine();
    Console.WriteLine("===== Condition Variable Scenario =====");
    Console.WriteLine($"Workers       : {workers}");
    Console.WriteLine($"Signal First  : {signalFirst}");
    Console.WriteLine($"Times blocked : {timesBlocked}");
    Console.WriteLine($"Value received: {value}");
    Console.WriteLine($"Wall time     : {wall.Elapsed.TotalMilliseconds:F3} ms");
    Console.WriteLine($"CPU time      : {cpu.Elapsed.TotalMilliseconds:F3} ms");
}

    static void RunSpinScenario()
    {
        dataReady = false;
        value = 0;

        Stopwatch wall = Stopwatch.StartNew();
        Stopwatch cpu = Stopwatch.StartNew();

        Thread worker = new Thread(() =>
        {
            // Poll until data becomes available.
            while (!dataReady)
            {
                Thread.Yield();
            }

            Console.WriteLine($"Worker received value: {value}");
        });

        worker.Start();

        Thread.Sleep(100);

        value = 42;
        dataReady = true;

        worker.Join();

        wall.Stop();
        cpu.Stop();

        Console.WriteLine();
        Console.WriteLine("===== Spin / Polling Scenario =====");
        Console.WriteLine("Workers       : 1");
        Console.WriteLine("Times blocked : 0");
        Console.WriteLine($"Value received: {value}");
        Console.WriteLine($"Wall time     : {wall.Elapsed.TotalMilliseconds:F3} ms");
        Console.WriteLine($"CPU time      : {cpu.Elapsed.TotalMilliseconds:F3} ms");
    }

    static void Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("Condition-Variable Signaling");
        Console.WriteLine("========================================");

        if (args.Length == 0)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("A: cond");
            Console.WriteLine("B: cond 1 signal-first");
            Console.WriteLine("C: cond 3");
            Console.WriteLine("D: spin");
            return;
        }

        string scenario = args[0].ToLower();

        switch (scenario)
        {
            case "cond":
                if (args.Length >= 3 &&
                    args[1] == "1" &&
                    args[2].ToLower() == "signal-first")
                {
                    // Scenario B
                    RunConditionScenario(1, true);
                }
                else if (args.Length >= 2 && args[1] == "3")
                {
                    // Scenario C
                    RunConditionScenario(3, false);
                }
                else
                {
                    // Scenario A
                    RunConditionScenario(1, false);
                }
                break;

            case "spin":
                // Scenario D
                RunSpinScenario();
                break;

            default:
                Console.WriteLine("Unknown scenario.");
                Console.WriteLine("Use: cond, cond 1 signal-first, cond 3, or spin.");
                break;
        }
    }
}