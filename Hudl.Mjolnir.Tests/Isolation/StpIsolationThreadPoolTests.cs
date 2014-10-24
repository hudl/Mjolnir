﻿using System;
using System.Threading;
using Hudl.Config;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Isolation;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Tests.Helper;
using Xunit;

namespace Hudl.Mjolnir.Tests.Isolation
{
    public class StpIsolationThreadPoolTests : TestFixture
    {
        [Fact]
        public void Enqueue_PoolSizeOneQueueSizeZero_AcceptsOneAndRejectsRemaining()
        {
            var pool = CreateAndStartPool(1, 0);
            pool.Enqueue(SleepTwoSeconds);
            Assert.Throws<IsolationThreadPoolRejectedException>(() =>
            {
                pool.Enqueue(SleepTwoSeconds);
            });
        }

        [Fact]
        public void Enqueue_PoolSizeOneQueueSizeOne_AcceptsTwoAndRejectsRemaining()
        {
            var pool = CreateAndStartPool(1, 1);
            pool.Enqueue(SleepTwoSeconds);
            pool.Enqueue(SleepTwoSeconds);
            Assert.Throws<IsolationThreadPoolRejectedException>(() =>
            {
                pool.Enqueue(SleepTwoSeconds);
            });
        }

        [Fact]
        public void Enqueue_PoolSizeFiveQueueSizeOne_AcceptsSixAndRejectsRemaining()
        {
            var pool = CreateAndStartPool(5, 1);
            pool.Enqueue(SleepTwoSeconds);
            pool.Enqueue(SleepTwoSeconds);
            pool.Enqueue(SleepTwoSeconds);
            pool.Enqueue(SleepTwoSeconds);
            pool.Enqueue(SleepTwoSeconds);
            pool.Enqueue(SleepTwoSeconds);
            Assert.Throws<IsolationThreadPoolRejectedException>(() =>
            {
                pool.Enqueue(SleepTwoSeconds);
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

        private object SleepTwoSeconds()
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
            var pool = new StpIsolationThreadPool(
                GroupKey.Named("Test"),
                new TransientConfigurableValue<int>(threadCount),
                new TransientConfigurableValue<int>(queueLength),
                new IgnoringStats());
            pool.Start();
            return pool;
        }
    }
}
