using System;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Hudl.Mjolnir.Bulkhead;
using Hudl.Mjolnir.Key;

namespace Hudl.Mjolnir.PerformanceTests
{
    public class BulkheadPerformanceTests
    {
        [Params(10, 20, 100, 200)]
        public int MaxConcurrency;
        private SemaphoreBulkhead _semaphoreBulkhead;

        [GlobalSetup]
        public void ConstructSemaphoreBulkhead()
        {
            _semaphoreBulkhead = new SemaphoreBulkhead(GroupKey.Named("test"), MaxConcurrency);

        }

        [Benchmark(Description = "Tests the throughput of a SemaphoreBulkhead's TryEnter method with varying settings for MaxConcurrency")]
        public async Task SemaphoreBulkheadTryEnterPerf()
        {
            var tasks = Enumerable.Range(1, 200).Select(_ =>
            {
                return Task.Run(() =>
                {
                    var wasSuccessful = TryEnterBulkhead();
                });
            });
            await Task.WhenAll(tasks.ToArray());
        }

        private bool TryEnterBulkhead()
        {
            return _semaphoreBulkhead.TryEnter();
        }
    }
}
