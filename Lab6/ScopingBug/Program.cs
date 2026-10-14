
using System;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    const int N = 2_000_000;

    static readonly int[] input = new int[N];
    static readonly int[] output = new int[N];

    static void Main(string[] args)
    {
        string mode = args.Length > 0 ? args[0] : "bug";
        int degree = args.Length > 1
            ? int.Parse(args[1])
            : Environment.ProcessorCount;

        ParallelOptions options = new ParallelOptions
        {
            MaxDegreeOfParallelism = degree
        };

        for (int i = 0; i < N; i++)
        {
            input[i] = i;
        }

        switch (mode)
        {
            case "bug":
            {
                int tmp = 0;

                Parallel.For(0, N, options, static i =>
                {
                    tmp = input[i] * 3;
                    Thread.SpinWait(20);
                    output[i] = tmp + 7;
                });

                break;
            }

            case "local":
            {
                Parallel.For(0, N, options, i =>
                {
                    int tmp = input[i] * 3;
                    Thread.SpinWait(20);
                    output[i] = tmp + 7;
                });

                break;
            }

            case "threadlocal":
            {
                Parallel.For(
                    0,
                    N,
                    options,
                    () => 0,
                    (i, state, tmp) =>
                    {
                        tmp = input[i] * 3;
                        Thread.SpinWait(20);
                        output[i] = tmp + 7;
                        return tmp;
                    },
                    tmp => { });

                break;
            }

            default:
                Console.WriteLine(
                    "mode must be bug, local or threadlocal");
                return;
        }

        long wrong = 0;

        for (int i = 0; i < N; i++)
        {
            if (output[i] != input[i] * 3 + 7)
            {
                wrong++;
            }
        }

        Console.WriteLine(
            $"[scoping] mode={mode} degree={degree} n={N} " +
            $"wrong={wrong} correct={(wrong == 0 ? "YES" : "NO")}");

        Console.WriteLine(
            $"Environment.ProcessorCount: {Environment.ProcessorCount}");
    }
}