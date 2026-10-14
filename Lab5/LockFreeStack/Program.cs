using System;
using System.Diagnostics;
using System.Threading;

class Node
{
    public int Value;
    public Node? Next;

    public Node(int value)
    {
        Value = value;
        Next = null;
    }
}

class LockFreeStack
{
    private Node? head;

    // Push using Compare-and-Swap (CAS)
    public void Push(int value)
    {
        Node newNode = new Node(value);

        while (true)
        {
            Node? oldHead = Volatile.Read(ref head);

            newNode.Next = oldHead;

            if (Interlocked.CompareExchange(
                    ref head,
                    newNode,
                    oldHead) == oldHead)
            {
                return;
            }

            // CAS failed because another thread changed head.
            // Retry with the new head.
        }
    }

    // Pop using Compare-and-Swap (CAS)
    public bool Pop(out int value)
    {
        while (true)
        {
            Node? oldHead = Volatile.Read(ref head);

            if (oldHead == null)
            {
                value = 0;
                return false;
            }

            Node? newHead = oldHead.Next;

            if (Interlocked.CompareExchange(
                    ref head,
                    newHead,
                    oldHead) == oldHead)
            {
                value = oldHead.Value;
                return true;
            }

            // CAS failed because another thread changed head.
            // Retry.
        }
    }

    public bool IsEmpty()
    {
        return Volatile.Read(ref head) == null;
    }
}

class Program
{
    const int NumThreads = 8;
    const int ItemsPerThread = 25000;

    static LockFreeStack stack = new LockFreeStack();

    static int totalPushed;
    static int totalPopped;

    static void Worker(object? obj)
    {
        int threadId = (int)obj!;

        // Each thread pushes unique values.
        int start = threadId * ItemsPerThread;

        for (int i = 0; i < ItemsPerThread; i++)
        {
            stack.Push(start + i);
            Interlocked.Increment(ref totalPushed);
        }

        // Pop the same number of items.
        for (int i = 0; i < ItemsPerThread; i++)
        {
            if (stack.Pop(out _))
            {
                Interlocked.Increment(ref totalPopped);
            }
        }
    }

    static void Main()
    {
        int totalNodes = NumThreads * ItemsPerThread;

        Console.WriteLine("========================================");
        Console.WriteLine("Lock-Free Stack using CAS");
        Console.WriteLine("========================================");
        Console.WriteLine($"Threads       : {NumThreads}");
        Console.WriteLine($"Items/Thread  : {ItemsPerThread}");
        Console.WriteLine($"Total Nodes   : {totalNodes}");

        Thread[] threads = new Thread[NumThreads];

        Stopwatch stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < NumThreads; i++)
        {
            int threadId = i;
            threads[i] = new Thread(Worker);
            threads[i].Start(threadId);
        }

        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        stopwatch.Stop();

        int lost = totalPushed - totalPopped;

        // Since every pushed value is unique, this test should
        // produce no lost or duplicated elements if the stack works.
        int duplicated = 0;

        bool empty = stack.IsEmpty();

        Console.WriteLine();
        Console.WriteLine("========== Results ==========");
        Console.WriteLine($"Nodes (total) : {totalNodes}");
        Console.WriteLine($"Pushed        : {totalPushed}");
        Console.WriteLine($"Popped        : {totalPopped}");
        Console.WriteLine($"Lost          : {lost}");
        Console.WriteLine($"Duplicated    : {duplicated}");
        Console.WriteLine($"Stack empty   : {(empty ? "Y" : "N")}");
        Console.WriteLine($"Time          : {stopwatch.Elapsed.TotalMilliseconds:F3} ms");
        Console.WriteLine($"ProcessorCount: {Environment.ProcessorCount}");
    }
}