using System;
using System.Threading;
using Hudl.Config;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.ThreadPool;
using Xunit;

namespace Hudl.Mjolnir.Tests.ThreadPool
{
    public class StpIsolationThreadPoolTests
    {
        [Fact]
        public void Enqueue_PoolSizeOneQueueSizeZero_AcceptsOneAndRejectsRemaining()
        {
            var pool = CreateAndStartPool(1, 0);
            pool.Enqueue(SleepThreeSeconds);
            Assert.Throws<IsolationThreadPoolRejectedException>(() =>
            {
                pool.Enqueue(SleepThreeSeconds);
            });
        }

        [Fact]
        public void Enqueue_PoolSizeOneQueueSizeOne_AcceptsTwoAndRejectsRemaining()
        {
            var pool = CreateAndStartPool(1, 1);
            pool.Enqueue(SleepThreeSeconds);
            pool.Enqueue(SleepThreeSeconds);
            Assert.Throws<IsolationThreadPoolRejectedException>(() =>
            {
                pool.Enqueue(SleepThreeSeconds);
            });
        }

        [Fact]
        public void Enqueue_PoolSizeFiveQueueSizeOne_AcceptsSixAndRejectsRemaining()
        {
            var pool = CreateAndStartPool(5, 1);
            pool.Enqueue(SleepThreeSeconds);
            pool.Enqueue(SleepThreeSeconds);
            pool.Enqueue(SleepThreeSeconds);
            pool.Enqueue(SleepThreeSeconds);
            pool.Enqueue(SleepThreeSeconds);
            pool.Enqueue(SleepThreeSeconds);
            Assert.Throws<IsolationThreadPoolRejectedException>(() =>
            {
                pool.Enqueue(SleepThreeSeconds);
            });
        }

        [Fact]
        public void Enqueue_AfterThreadsReleased_AcceptsMore()
        {
            var pool = CreateAndStartPool(1, 0);
            pool.Enqueue(ReturnImmediately);
            Thread.Sleep(10);
            pool.Enqueue(ReturnImmediately); // Shouldn't be rejected.
        }

        private object SleepThreeSeconds()
        {
            Thread.Sleep(TimeSpan.FromSeconds(2));
            return new {};
        }

        private object ReturnImmediately()
        {
            return new { };
        }

        private StpIsolationThreadPool CreateAndStartPool(int threadCount, int queueLength)
        {
            var pool = new StpIsolationThreadPool(GroupKey.Named("Test"), new TransientConfigurableValue<int>(threadCount), new TransientConfigurableValue<int>(queueLength));
            pool.Start();
            return pool;
        }
    }
}
