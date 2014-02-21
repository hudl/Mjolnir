using System;
using System.Threading.Tasks;
using Hudl.Config;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Tests.Helper;
using Hudl.Mjolnir.ThreadPool;
using Hudl.Riemann;
using Moq;
using Xunit;

namespace Hudl.Mjolnir.Tests.Stats
{
    public class SemaphoreSlimIsolationSemaphoreStatsTests : TestFixture
    {
        [Fact]
        public async Task Construct_CreatesGauges()
        {
            const long gaugeIntervalMillis = 50;

            var mockRiemann = new Mock<IRiemann>();
            var semaphore = new SemaphoreSlimIsolationSemaphore(GroupKey.Named("Test"), new TransientConfigurableValue<int>(10), mockRiemann.Object, new TransientConfigurableValue<long>(gaugeIntervalMillis));

            await Task.Delay(TimeSpan.FromMilliseconds(gaugeIntervalMillis + 50));

            mockRiemann.Verify(m => m.ConfigGauge("mjolnir fallback-semaphore Test conf.maxConcurrent", 10), Times.AtLeastOnce);
            mockRiemann.Verify(m => m.Gauge("mjolnir fallback-semaphore Test available", "Available", 10, null, null, null), Times.AtLeastOnce);
        }
    }
}
