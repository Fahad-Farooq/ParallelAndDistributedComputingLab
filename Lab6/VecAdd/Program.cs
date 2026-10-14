
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

class Program
{
    const int N = 5_000_000;
    const int Reps = 20;

    static readonly double[] A = new double[N];
    static readonly double[] B = new double[N];
    static readonly double[] C = new double[N];

    static void VecAdd(ParallelOptions options)
    {
        Parallel.ForEach(
            Partitioner.Create(0, N),
            options,
            static range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    C[i] = A[i] + B[i];
                }
            });
    }

    static void Main(string[] args)
    {
        int degree = args.Length > 0
            ? int.Parse(args[0])
            : Environment.ProcessorCount;

        ParallelOptions options = new ParallelOptions
        {
            MaxDegreeOfParallelism = degree
        };

        for (int i = 0; i < N; i++)
        {
            A[i] = 0.5 * i;
            B[i] = 1.5 * i;
        }

        VecAdd(options); // Warm-up pass

        Stopwatch sw = Stopwatch.StartNew();

        for (int r = 0; r < Reps; r++)
        {
            VecAdd(options);
        }

        sw.Stop();

        long mismatches = 0;

        for (int i = 0; i < N; i++)
        {
            if (C[i] != 2.0 * i)
            {
                mismatches++;
            }
        }

        string ok = mismatches == 0 ? "YES" : "NO";

        Console.WriteLine(
            $"[vecadd] degree={degree} n={N} reps={Reps} " +
            $"mismatches={mismatches} correct={ok} " +
            $"time={sw.Elapsed.TotalMilliseconds:F2} ms");
    }
}