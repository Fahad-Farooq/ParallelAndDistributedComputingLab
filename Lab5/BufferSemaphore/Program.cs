using System;
using System.Threading;

class Program
{
    // =========================
    // Shared buffer
    // =========================
    static int[] buffer = null!;
    static int bufferSize;

    static int inPos = 0;
    static int outPos = 0;

    static readonly object gate = new object();

    // =========================
    // Semaphores
    // =========================
    static SemaphoreSlim emptySlots = null!;
    static SemaphoreSlim fullSlots = null!;

    // =========================
    // Verification
    // =========================
    static int[] seen = null!;
    static readonly object seenGate = new object();

    static int lost = 0;
    static int duplicated = 0;

    // =========================
    // Configuration
    // =========================
    static int producerCount;
    static int consumerCount;

    // Each producer creates this many items.
    const int ItemsPerProducer = 25000;

    // =========================
    // Producer
    // =========================
    static void Producer(object? obj)
    {
        int producerId = (int)obj!;

        int startItem = producerId * ItemsPerProducer;
        int endItem = startItem + ItemsPerProducer;

        for (int item = startItem; item < endItem; item++)
        {
            // Wait until an empty buffer slot is available.
            emptySlots.Wait();

            // Protect the circular-buffer index and insertion.
            lock (gate)
            {
                buffer[inPos] = item;
                inPos = (inPos + 1) % bufferSize;
            }

            // Signal that a full slot is now available.
            fullSlots.Release();
        }
    }

    // =========================
    // Consumer Deadlock
    // =========================
 /*static void Consumer(object? obj)
{
    for (int i = 0; i < (producerCount * ItemsPerProducer) / consumerCount; i++)
    {
        // WRONG ORDER — DEADLOCK EXPERIMENT
        lock (gate)
        {
            fullSlots.Wait();

            int item = buffer[outPos];
            outPos = (outPos + 1) % bufferSize;

            lock (seenGate)
            {
                if (seen[item] == 0)
                    seen[item] = 1;
                else
                    duplicated++;
            }

            emptySlots.Release();
        }
    }
}*/
// =========================
    // Consumer Deadlock
    // =========================
static void Consumer(object? obj)
{
    for (int i = 0; i < (producerCount * ItemsPerProducer) / consumerCount; i++)
    {
        fullSlots.Wait();

        int item;

        lock (gate)
        {
            item = buffer[outPos];
            outPos = (outPos + 1) % bufferSize;
        }

        lock (seenGate)
        {
            if (seen[item] == 0)
                seen[item] = 1;
            else
                duplicated++;
        }

        emptySlots.Release();
    }
}

    // =========================
    // Main
    // =========================
    static void Main(string[] args)
    {
        // Require:
        // dotnet run --no-build -c Release -- <producers> <consumers> <capacity>

        if (args.Length != 3)
        {
            Console.WriteLine(
                "Usage: dotnet run --no-build -c Release -- <producers> <consumers> <capacity>"
            );
            return;
        }

        producerCount = int.Parse(args[0]);
        consumerCount = int.Parse(args[1]);
        bufferSize = int.Parse(args[2]);

        int totalItems = producerCount * ItemsPerProducer;

        // =========================
        // Initialize buffer
        // =========================
        buffer = new int[bufferSize];

        // emptySlots starts at capacity because
        // initially every buffer slot is empty.
        emptySlots = new SemaphoreSlim(bufferSize, bufferSize);

        // fullSlots starts at 0 because
        // initially there are no produced items.
        fullSlots = new SemaphoreSlim(0, bufferSize);

        // Used to detect lost and duplicated items.
        seen = new int[totalItems];

        Console.WriteLine("========================================");
        Console.WriteLine("Semaphore Producer-Consumer Buffer");
        Console.WriteLine("========================================");
        Console.WriteLine($"Producers       : {producerCount}");
        Console.WriteLine($"Consumers       : {consumerCount}");
        Console.WriteLine($"Buffer Size     : {bufferSize}");
        Console.WriteLine($"Items/Producer  : {ItemsPerProducer}");
        Console.WriteLine($"Total Items     : {totalItems}");
        Console.WriteLine();

        Thread[] producers = new Thread[producerCount];
        Thread[] consumers = new Thread[consumerCount];

        // =========================
        // Create producer threads
        // =========================
        for (int i = 0; i < producerCount; i++)
        {
            producers[i] = new Thread(Producer);
        }

        // =========================
        // Create consumer threads
        // =========================
        for (int i = 0; i < consumerCount; i++)
        {
            consumers[i] = new Thread(Consumer);
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Start consumers
        for (int i = 0; i < consumerCount; i++)
        {
            consumers[i].Start();
        }

        // Start producers
        for (int i = 0; i < producerCount; i++)
        {
            producers[i].Start(i);
        }

        // Wait for producers
        for (int i = 0; i < producerCount; i++)
        {
            producers[i].Join();
        }

        // Wait for consumers
        for (int i = 0; i < consumerCount; i++)
        {
            consumers[i].Join();
        }

        stopwatch.Stop();

        // =========================
        // Count lost items
        // =========================
        for (int i = 0; i < totalItems; i++)
        {
            if (seen[i] == 0)
            {
                lost++;
            }
        }

        // =========================
        // Results
        // =========================
        Console.WriteLine("--------------- Results ---------------");
        Console.WriteLine($"Total items    : {totalItems}");
        Console.WriteLine($"Lost           : {lost}");
        Console.WriteLine($"Duplicated     : {duplicated}");
        Console.WriteLine($"Time (ms)      : {stopwatch.Elapsed.TotalMilliseconds:F2}");
        Console.WriteLine("----------------------------------------");
    }
}