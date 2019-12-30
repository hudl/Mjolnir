using System;
using BenchmarkDotNet.Running;

namespace Hudl.Mjolnir.PerformanceTests
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<BulkheadPerformanceTests>();
        }
    }
}
