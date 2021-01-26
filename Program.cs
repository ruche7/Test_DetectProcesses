using System;
using System.Buffers;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace TestApp
{
    public class BenchmarkMethods
    {
        private Process[] processes = Array.Empty<Process>();

        private Process[]? results;

        private string targetProduct = @"";

        [IterationSetup]
        public void Setup()
        {
            this.processes = Process.GetProcesses();
            this.targetProduct = @"foo";
        }

        [Benchmark]
        public void ByReadOnlySpan()
        {
            ReadOnlySpan<Process> procs = this.processes;

            var temp = ArrayPool<Process>.Shared.Rent(procs.Length);
            try
            {
                var target = this.targetProduct;

                int count = 0;
                for (int pi = 0; pi < procs.Length; ++pi)
                {
                    var p = procs[pi];
                    if (IsMatch(p, target))
                    {
                        temp[count] = p;
                        ++count;
                    }
                }

                Array.Resize(ref this.results, count);
                Array.Copy(temp, this.results, count);
            }
            finally
            {
                ArrayPool<Process>.Shared.Return(temp);
            }
        }

        [Benchmark]
        public void ByLinq()
        {
            var target = this.targetProduct;
            this.results = this.processes.Where(p => IsMatch(p, target)).ToArray();
        }

        [Benchmark]
        public void ByParallelLinq()
        {
            var target = this.targetProduct;
            this.results =
                this.processes.AsParallel().Where(p => IsMatch(p, target)).ToArray();
        }

        [Benchmark]
        public void ByParallelOrderedLinq()
        {
            var target = this.targetProduct;
            this.results =
                this.processes
                    .AsParallel()
                    .AsOrdered()
                    .Where(p => IsMatch(p, target))
                    .ToArray();
        }

        private static bool IsMatch(Process process, string targetProduct)
        {
            try
            {
                return (process.MainModule?.FileVersionInfo.ProductName == targetProduct);
            }
            catch { }
            return false;
        }
    }

    internal static class Program
    {
        private static void Main()
        {
            BenchmarkRunner.Run<BenchmarkMethods>();

            Console.WriteLine(@"Process count : " + Process.GetProcesses().Length);
        }
    }
}
