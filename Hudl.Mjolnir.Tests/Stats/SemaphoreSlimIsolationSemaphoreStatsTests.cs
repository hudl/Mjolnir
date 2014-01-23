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
            var mockRiemann = new Mock<IRiemann>();
            var semaphore = new SemaphoreSlimIsolationSemaphore(GroupKey.Named("Test"), new TransientConfigurableValue<int>(10), mockRiemann.Object);

            await Task.Delay(TimeSpan.FromMilliseconds(5010)); // TODO This is jank, but the interval's not injectable at the moment.

            mockRiemann.Verify(m => m.ConfigGauge("mjolnir fallback-semaphore Test conf.maxConcurrent", 10), Times.Once);
            mockRiemann.Verify(m => m.Gauge("mjolnir fallback-semaphore Test available", "Available", 10, null, null, null), Times.Once);
        }
    }
}
