
using System;
using System.Diagnostics;
using System.Threading.Tasks;

class Program
{
    const long SeriesTerms = 150_000_000;
    const int PrimeLimit = 300_000;
    const int CollatzMax = 1_000_000;

    // Operation A: Sum the series 1/k^2
    static double SeriesSum()
    {
        double s = 0.0;

        for (long k = 1; k <= SeriesTerms; k++)
            s += 1.0 / ((double)k * (double)k);

        return s;
    }

    // Operation B: Count primes using trial division
    static long CountPrimes()
    {
        long count = 0;

        for (long n = 2; n < PrimeLimit; n++)
        {
            bool prime = true;

            for (long d = 2; d * d <= n; d++)
            {
                if (n % d == 0)
                {
                    prime = false;
                    break;
                }
            }

            if (prime)
                count++;
        }

        return count;
    }

    // Operation C: Find the starting number with the
    // longest Collatz chain below CollatzMax
    static long LongestCollatz()
    {
        long bestN = 1;
        long bestLen = 1;

        for (long n = 1; n < CollatzMax; n++)
        {
            long x = n;
            long len = 1;

            while (x != 1)
            {
                x = (x % 2 != 0) ? 3 * x + 1 : x / 2;
                len++;
            }

            if (len > bestLen)
            {
                bestLen = len;
                bestN = n;
            }
        }

        return bestN;
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

        // Run all operations serially to obtain reference results
        Stopwatch total = Stopwatch.StartNew();

        Stopwatch sw = Stopwatch.StartNew();
        double sa = SeriesSum();
        double sA = sw.Elapsed.TotalSeconds;

        sw.Restart();
        long sb = CountPrimes();
        double sB = sw.Elapsed.TotalSeconds;

        sw.Restart();
        long sc = LongestCollatz();
        double sC = sw.Elapsed.TotalSeconds;

        double serial = total.Elapsed.TotalSeconds;

        // Results, action times, and thread IDs
        double ra = 0;
        long rb = 0, rc = 0;

        double tA = 0, tB = 0, tC = 0;
        int thA = -1, thB = -1, thC = -1;

        // Run the three independent operations concurrently
        total.Restart();

        Parallel.Invoke(
            options,

            // Operation A
            () =>
            {
                Stopwatch w = Stopwatch.StartNew();
                ra = SeriesSum();
                tA = w.Elapsed.TotalSeconds;
                thA = Environment.CurrentManagedThreadId;
            },

            // Operation B
            () =>
            {
                Stopwatch w = Stopwatch.StartNew();
                rb = CountPrimes();
                tB = w.Elapsed.TotalSeconds;
                thB = Environment.CurrentManagedThreadId;
            },

            // Operation C
            () =>
            {
                Stopwatch w = Stopwatch.StartNew();
                rc = LongestCollatz();
                tC = w.Elapsed.TotalSeconds;
                thC = Environment.CurrentManagedThreadId;
            }
        );

        double wall = total.Elapsed.TotalSeconds;

        Console.WriteLine(
            $"serial : A={sA:F2} s B={sB:F2} s " +
            $"C={sC:F2} s total={serial:F2} s"
        );

        Console.WriteLine(
            $"actions : degree={degree} " +
            $"A={tA:F2} s (thread {thA}) " +
            $"B={tB:F2} s (thread {thB}) " +
            $"C={tC:F2} s (thread {thC})"
        );

        bool match = ra == sa && rb == sb && rc == sc;

        Console.WriteLine(
            $"wall={wall:F2} s speedup={serial / wall:F2} " +
            $"results_match={(match ? "YES" : "NO")}"
        );
    }
}