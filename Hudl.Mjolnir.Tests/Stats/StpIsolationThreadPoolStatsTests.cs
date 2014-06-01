using System;
using System.Threading.Tasks;
using Hudl.Config;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Tests.Helper;
using Hudl.Mjolnir.ThreadPool;
using Moq;
using Xunit;

namespace Hudl.Mjolnir.Tests.Stats
{
    public class StpIsolationThreadPoolStatsTests : TestFixture
    {
        [Fact]
        public async Task Construct_CreatesGauges()
        {
            const long gaugeIntervalMillis = 50;

            var mockStats = new Mock<IStats>();
            var pool = new StpIsolationThreadPool(
                GroupKey.Named("Test"),
                new TransientConfigurableValue<int>(10),
                new TransientConfigurableValue<int>(20),
                mockStats.Object,
                new TransientConfigurableValue<long>(gaugeIntervalMillis));

            await Task.Delay(TimeSpan.FromMilliseconds(gaugeIntervalMillis + 50));

            mockStats.Verify(m => m.Gauge("mjolnir pool Test activeThreads", null, It.IsAny<long>()), Times.AtLeastOnce);
            mockStats.Verify(m => m.Gauge("mjolnir pool Test inUseThreads", null, It.IsAny<long>()), Times.AtLeastOnce);
            mockStats.Verify(m => m.Gauge("mjolnir pool Test pendingCompletion", null, It.IsAny<long>()), Times.AtLeastOnce);
            mockStats.Verify(m => m.ConfigGauge("mjolnir pool Test conf.threadCount", 10), Times.AtLeastOnce);
            mockStats.Verify(m => m.ConfigGauge("mjolnir pool Test conf.queueLength", 20), Times.AtLeastOnce);
        }

        // TODO This isn't deterministic, it can fail depending on how/when it gets scheduled. Would be nice to test, though.
        //[Fact]
        //public async Task Construct_ThreadPoolInitialization()
        //{
        //    const int threads = 4;

        //    var mockStats = new Mock<IStats>();
        //    var pool = new StpIsolationThreadPool(
        //        GroupKey.Named("Test"),
        //        new TransientConfigurableValue<int>(threads),
        //        new TransientConfigurableValue<int>(20),
        //        mockStats.Object);

        //    
        //    await Task.Delay(TimeSpan.FromMilliseconds(3000)); // Brief pause to let pool spin up threads.

        //    mockStats.Verify(m => m.Event("mjolnir pool Test thread", "Initialized", null, null, null, null), Times.Exactly(threads));
        //}

        [Fact]
        public void Start_Elapsed()
        {
            var mockStats = new Mock<IStats>();
            var pool = new StpIsolationThreadPool(
                GroupKey.Named("Test"),
                new TransientConfigurableValue<int>(10),
                new TransientConfigurableValue<int>(20),
                mockStats.Object);

            pool.Start();

            mockStats.Verify(m => m.Elapsed("mjolnir pool Test Start", null, It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public void Enqueue_Elapsed()
        {
            var mockStats = new Mock<IStats>();
            var pool = new StpIsolationThreadPool(
                GroupKey.Named("Test"),
                new TransientConfigurableValue<int>(10),
                new TransientConfigurableValue<int>(20),
                mockStats.Object);
            pool.Start();

            pool.Enqueue<object>(() => new { });

            mockStats.Verify(m => m.Elapsed("mjolnir pool Test Enqueue", "Enqueued", It.IsAny<TimeSpan>()), Times.Once);
        }
    }
}
