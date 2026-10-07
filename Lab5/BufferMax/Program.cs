using System;
using System.Threading;

class Program
{
    // =========================
    // Configuration
    // =========================
    const int BufferSize = 8;
    const int ItemCount = 100000;

    // =========================
    // Shared circular buffer
    // =========================
    static readonly int[] buffer = new int[BufferSize];

    static int inPos = 0;
    static int outPos = 0;
    static int count = 0;

    // Mutex-style lock
    static readonly object gate = new object();

    // =========================
    // Statistics
    // =========================
    static long producerRetries = 0;
    static long consumerRetries = 0;

    static int orderErrors = 0;
    static long checksum = 0;

    // Used to verify FIFO order
    static int expectedValue = 0;

    // =========================
    // Producer
    // =========================
    static void Producer()
    {
        for (int item = 0; item < ItemCount;)
        {
            bool inserted = false;

            lock (gate)
            {
                if (count < BufferSize)
                {
                    // Insert item into circular buffer
                    buffer[inPos] = item;

                    // Move insertion position
                    inPos = (inPos + 1) % BufferSize;

                    // Increase number of items
                    count++;

                    inserted = true;
                }
            }

            if (inserted)
            {
                item++;
            }
            else
            {
                // Buffer is full
                producerRetries++;

                // Give another thread a chance to run
                Thread.Yield();
            }
        }
    }

    // =========================
    // Consumer
    // =========================
    static void Consumer()
    {
        for (int i = 0; i < ItemCount;)
        {
            bool removed = false;
            int item = 0;

            lock (gate)
            {
                if (count > 0)
                {
                    // Remove item from circular buffer
                    item = buffer[outPos];

                    // Move removal position
                    outPos = (outPos + 1) % BufferSize;

                    // Decrease number of items
                    count--;

                    removed = true;
                }
            }

            if (removed)
            {
                // Check FIFO ordering
                if (item != expectedValue)
                {
                    orderErrors++;
                }

                expectedValue++;

                // Add item to checksum
                checksum += item;

                i++;
            }
            else
            {
                // Buffer is empty
                consumerRetries++;

                // Give another thread a chance to run
                Thread.Yield();
            }
        }
    }

    // =========================
    // Main
    // =========================
    static void Main()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("Mutex-Protected Bounded Buffer");
        Console.WriteLine("========================================");
        Console.WriteLine($"Buffer Size : {BufferSize}");
        Console.WriteLine($"Items       : {ItemCount}");
        Console.WriteLine();

        // Expected checksum:
        // 0 + 1 + 2 + ... + (ItemCount - 1)
        long expectedChecksum =
            (long)ItemCount * (ItemCount - 1) / 2;

        // Create producer and consumer threads
        Thread producer = new Thread(Producer);
        Thread consumer = new Thread(Consumer);

        // Start timing
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        producer.Start();
        consumer.Start();

        // Wait for both threads to finish
        producer.Join();
        consumer.Join();

        stopwatch.Stop();

        // Checksum verification
        bool checksumOK = checksum == expectedChecksum;

        // Final results
        Console.WriteLine("--------------- Results ---------------");
        Console.WriteLine($"Items          : {ItemCount}");
        Console.WriteLine($"Checksum       : {checksum}");
        Console.WriteLine($"Expected       : {expectedChecksum}");
        Console.WriteLine($"Checksum OK    : {(checksumOK ? "Y" : "N")}");
        Console.WriteLine($"Order errors   : {orderErrors}");
        Console.WriteLine($"Producer retries : {producerRetries}");
        Console.WriteLine($"Consumer retries : {consumerRetries}");
        Console.WriteLine($"Time (ms)      : {stopwatch.Elapsed.TotalMilliseconds:F2}");
        Console.WriteLine("----------------------------------------");
    }
}