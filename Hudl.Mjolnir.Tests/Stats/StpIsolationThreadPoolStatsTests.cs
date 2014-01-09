using System;
using System.Threading.Tasks;
using Hudl.Config;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.ThreadPool;
using Hudl.Riemann;
using Moq;
using Xunit;

namespace Hudl.Mjolnir.Tests.Stats
{
    public class StpIsolationThreadPoolStatsTests
    {
        [Fact]
        public async Task Construct_CreatesGauges()
        {
            var mockRiemann = new Mock<IRiemann>();
            var pool = new StpIsolationThreadPool(
                GroupKey.Named("Test"),
                new TransientConfigurableValue<int>(10),
                new TransientConfigurableValue<int>(20),
                mockRiemann.Object);

            await Task.Delay(TimeSpan.FromMilliseconds(5010)); // TODO This is jank, but the interval's not injectable at the moment.

            mockRiemann.Verify(m => m.Gauge("mjolnir pool Test activeThreads", null, It.IsAny<long>(), null, null, null), Times.Once);
            mockRiemann.Verify(m => m.Gauge("mjolnir pool Test inUseThreads", null, It.IsAny<long>(), null, null, null), Times.Once);
            mockRiemann.Verify(m => m.Gauge("mjolnir pool Test pendingCompletion", null, It.IsAny<long>(), null, null, null), Times.Once);
            mockRiemann.Verify(m => m.ConfigGauge("mjolnir pool Test conf.threadCount", 10), Times.Once);
            mockRiemann.Verify(m => m.ConfigGauge("mjolnir pool Test conf.queueLength", 20), Times.Once);
        }

        // TODO This isn't deterministic, it can fail depending on how/when it gets scheduled. Would be nice to test, though.
        //[Fact]
        //public async Task Construct_ThreadPoolInitialization()
        //{
        //    const int threads = 4;

        //    var mockRiemann = new Mock<IRiemann>();
        //    var pool = new StpIsolationThreadPool(
        //        GroupKey.Named("Test"),
        //        new TransientConfigurableValue<int>(threads),
        //        new TransientConfigurableValue<int>(20),
        //        mockRiemann.Object);

        //    
        //    await Task.Delay(TimeSpan.FromMilliseconds(3000)); // Brief pause to let pool spin up threads.

        //    mockRiemann.Verify(m => m.Event("mjolnir pool Test thread", "Initialized", null, null, null, null), Times.Exactly(threads));
        //}

        [Fact]
        public void Start_Elapsed()
        {
            var mockRiemann = new Mock<IRiemann>();
            var pool = new StpIsolationThreadPool(
                GroupKey.Named("Test"),
                new TransientConfigurableValue<int>(10),
                new TransientConfigurableValue<int>(20),
                mockRiemann.Object);

            pool.Start();

            mockRiemann.Verify(m => m.Elapsed("mjolnir pool Test Start", null, It.IsAny<TimeSpan>(), null, null, null), Times.Once);
        }

        [Fact]
        public void Enqueue_Elapsed()
        {
            var mockRiemann = new Mock<IRiemann>();
            var pool = new StpIsolationThreadPool(
                GroupKey.Named("Test"),
                new TransientConfigurableValue<int>(10),
                new TransientConfigurableValue<int>(20),
                mockRiemann.Object);
            pool.Start();

            pool.Enqueue<object>(() => new { });

            mockRiemann.Verify(m => m.Elapsed("mjolnir pool Test Enqueue", "Enqueued", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
        }
    }
}
