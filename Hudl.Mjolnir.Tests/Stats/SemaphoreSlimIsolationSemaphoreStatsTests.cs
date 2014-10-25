using System;
using System.Threading.Tasks;
using Hudl.Config;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Isolation;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Tests.Helper;
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

            var mockStats = new Mock<IStats>();
            var semaphore = new SemaphoreSlimIsolationSemaphore(GroupKey.Named("Test"), new TransientConfigurableValue<int>(10), mockStats.Object, new TransientConfigurableValue<long>(gaugeIntervalMillis));

            await Task.Delay(TimeSpan.FromMilliseconds(gaugeIntervalMillis + 50));

            mockStats.Verify(m => m.Gauge("mjolnir fallback-semaphore Test available", "Available", 10), Times.AtLeastOnce);
        }
    }
}
