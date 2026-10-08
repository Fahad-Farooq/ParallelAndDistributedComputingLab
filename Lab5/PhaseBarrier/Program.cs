using System;
using System.Diagnostics;
using System.Threading;

class PhaseBarrier
{
    private readonly int parties;
    private int waiting;
    private int generation;

    private readonly object locker = new object();

    public PhaseBarrier(int parties)
    {
        this.parties = parties;
        waiting = 0;
        generation = 0;
    }

    public void SignalAndWait()
    {
        lock (locker)
        {
            int myGeneration = generation;

            waiting++;

            if (waiting == parties)
            {
                // Last worker arrives.
                // Start the next generation.
                waiting = 0;
                generation++;

                Monitor.PulseAll(locker);
            }
            else
            {
                // Wait until the current generation is complete.
                while (myGeneration == generation)
                {
                    Monitor.Wait(locker);
                }
            }
        }
    }
}

class Program
{
    const int DefaultWorkers = 4;
    const int Rounds = 5;

    static int[] phaseData = Array.Empty<int>();
    static int[] result = Array.Empty<int>();

    static PhaseBarrier barrier = null!;

    static void WorkerWithBarrier(object? obj)
    {
        int workerId = (int)obj!;
        int workers = phaseData.Length;

        for (int round = 1; round <= Rounds; round++)
        {
            // Phase 1:
            // Each worker publishes its value for this round.
            phaseData[workerId] = round;

            // All workers must finish publishing before
            // anyone reads the complete set of values.
            barrier.SignalAndWait();

            // Phase 2:
            // Calculate the sum of all workers' values.
            int total = 0;

            for (int i = 0; i < workers; i++)
            {
                total += phaseData[i];
            }

            result[workerId] = total;

            // All workers must finish reading/calculating
            // before anyone starts the next round.
            barrier.SignalAndWait();
        }
    }

    static void WorkerWithoutBarrier(object? obj)
    {
        int workerId = (int)obj!;
        int workers = phaseData.Length;

        for (int round = 1; round <= Rounds; round++)
        {
            // Publish this worker's value.
            phaseData[workerId] = round;

            // Encourage different workers to progress
            // at different speeds.
            Thread.Yield();

            // No barrier:
            // A worker may read while other workers are
            // still writing their values.
            int total = 0;

            for (int i = 0; i < workers; i++)
            {
                total += phaseData[i];
            }

            result[workerId] = total;

            Thread.Yield();
        }
    }

    static bool CheckResult(int workers, out int mismatchingCells)
    {
        mismatchingCells = 0;

        // After the final round, every worker should have
        // calculated workers * Rounds.
        int expected = workers * Rounds;

        for (int i = 0; i < workers; i++)
        {
            if (result[i] != expected)
            {
                mismatchingCells++;
            }
        }

        return mismatchingCells == 0;
    }

    static double Run(bool useBarrier, int workers)
    {
        phaseData = new int[workers];
        result = new int[workers];

        barrier = new PhaseBarrier(workers);

        Thread[] threads = new Thread[workers];

        Stopwatch stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < workers; i++)
        {
            int workerId = i;

            if (useBarrier)
            {
                threads[i] = new Thread(WorkerWithBarrier);
            }
            else
            {
                threads[i] = new Thread(WorkerWithoutBarrier);
            }

            threads[i].Start(workerId);
        }

        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        stopwatch.Stop();

        bool correct = CheckResult(workers, out int mismatches);

        Console.WriteLine();
        Console.WriteLine("========================================");

        if (useBarrier)
            Console.WriteLine("PHASE BARRIER VERSION");
        else
            Console.WriteLine("NO-BARRIER VERSION");

        Console.WriteLine("========================================");
        Console.WriteLine($"Workers            : {workers}");
        Console.WriteLine($"Rounds             : {Rounds}");
        Console.WriteLine($"Correct?           : {(correct ? "Y" : "N")}");
        Console.WriteLine($"Mismatching cells  : {mismatches}");
        Console.WriteLine($"Time               : {stopwatch.Elapsed.TotalMilliseconds:F3} ms");
        Console.WriteLine($"Environment.ProcessorCount: {Environment.ProcessorCount}");

        return stopwatch.Elapsed.TotalMilliseconds;
    }

    static void Main(string[] args)
    {
        bool noBarrier = false;
        int workers = DefaultWorkers;

        if (args.Length > 0)
        {
            if (args[0].Equals("--no-barrier",
                StringComparison.OrdinalIgnoreCase))
            {
                noBarrier = true;

                // Optional worker count:
                // --no-barrier 8
                if (args.Length > 1 &&
                    int.TryParse(args[1], out int parsedWorkers))
                {
                    workers = parsedWorkers;
                }
            }
            else if (int.TryParse(args[0], out int parsedWorkers))
            {
                // Barrier version:
                // 8
                workers = parsedWorkers;
            }
        }

        if (workers < 4)
        {
            Console.WriteLine("Error: workers must be 4 or more.");
            return;
        }

        Run(!noBarrier, workers);
    }
}