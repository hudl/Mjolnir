using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Hudl.Config;
using Hudl.Mjolnir.Isolation;
using Xunit;

namespace Hudl.Mjolnir.Tests.Isolation
{
    public class TaskSchedulerQueuedIsolationTests
    {
        [Fact]
        public void Construct_MaxConcurrency_ArgumentValidation()
        {
            var maxQueueLength = new TransientConfigurableValue<int>(1); // Good value for queue length, not testing it here.

            Assert.Throws<ArgumentOutOfRangeException>(() => new QueuedTaskSchedulerIsolationStrategy(new TransientConfigurableValue<int>(0), maxQueueLength));
            Assert.Throws<ArgumentOutOfRangeException>(() => new QueuedTaskSchedulerIsolationStrategy(new TransientConfigurableValue<int>(-1), maxQueueLength));
            Assert.Throws<ArgumentOutOfRangeException>(() => new QueuedTaskSchedulerIsolationStrategy(new TransientConfigurableValue<int>(Int32.MinValue), maxQueueLength));
            Assert.DoesNotThrow(() => new QueuedTaskSchedulerIsolationStrategy(new TransientConfigurableValue<int>(1), maxQueueLength));
            Assert.DoesNotThrow(() => new QueuedTaskSchedulerIsolationStrategy(new TransientConfigurableValue<int>(Int32.MaxValue), maxQueueLength));
        }

        [Fact]
        public void Construct_MaxQueueLength_ArgumentValidation()
        {
            var maxConcurrency = new TransientConfigurableValue<int>(1); // Good value for max concurrency, not testing it here.

            Assert.Throws<ArgumentOutOfRangeException>(() => new QueuedTaskSchedulerIsolationStrategy(maxConcurrency, new TransientConfigurableValue<int>(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new QueuedTaskSchedulerIsolationStrategy(maxConcurrency, new TransientConfigurableValue<int>(Int32.MinValue)));
            Assert.DoesNotThrow(() => new QueuedTaskSchedulerIsolationStrategy(maxConcurrency, new TransientConfigurableValue<int>(0)));
            Assert.DoesNotThrow(() => new QueuedTaskSchedulerIsolationStrategy(maxConcurrency, new TransientConfigurableValue<int>(1)));
            Assert.DoesNotThrow(() => new QueuedTaskSchedulerIsolationStrategy(maxConcurrency, new TransientConfigurableValue<int>(Int32.MaxValue)));
        }

        [Fact]
        public async Task Enqueue_MaxConcurrencyOne_QueueLengthZero_AcceptsOneAndRejectsRemaining()
        {
            await RunEnqueueTest(1, 0);
        }

        [Fact]
        public async Task Enqueue_MaxConcurrencyOne_QueueLengthOne_AcceptsTwoAndRejectsRemaining()
        {
            await RunEnqueueTest(1, 1);
        }

        [Fact]
        public async Task Enqueue_MaxConcurrencyFive_QueueLengthOne_AcceptsSixAndRejectsRemaining()
        {
            await RunEnqueueTest(5, 1);
        }

        [Fact]
        public void Enqueue_AfterOneTaskCompletes_AcceptsMore()
        {
            var isolation = CreateIsolation(1, 0);
            Assert.DoesNotThrow(() => isolation.Enqueue(ReturnImmediately, CancellationToken.None));
            Thread.Sleep(10); // Make sure it completes (by hacky-sleeping and hoping that's enough).
            Assert.DoesNotThrow(() => isolation.Enqueue(ReturnImmediately, CancellationToken.None));
        }

        // TODO Make sure that the counts are maintained when a task throws exceptions.
        // - Run a test that counts a bunch and verify that it zeroes out under high concurrency after it's done.

        // TODO Consideration for attached/detached child tasks.

        // TODO Consideration for ThreadPool size. Should we actually be using the application pool, or instead manage our own?
        // - A risk point is pushing up on the application pool max (or current size). How fast does it scale?
        // - Get some metrics on current pool usage in production.

        // TODO Consider using HideScheduler as a TaskCreationOption on the scheduler

        private async Task RunEnqueueTest(int maxConcurrency, int maxQueueLength)
        {
            LogTime("Started");
            var isolation = CreateIsolation(maxConcurrency, maxQueueLength);

            var tasks = new List<Task<object>>();
            for (var i = 0; i < maxConcurrency + maxQueueLength; i++)
            {
                Assert.DoesNotThrow(() => tasks.Add(isolation.Enqueue(SleepOneSecond, CancellationToken.None)));
            }

            var e = Assert.Throws<IsolationStrategyRejectedException>(() => tasks.Add(isolation.Enqueue(SleepOneSecond, CancellationToken.None)));

            // Assumes *none* of the tasks have completed yet (an assumption that all of these tests make).
            Assert.Equal(maxConcurrency + maxQueueLength, e.Data[QueueLengthExceededException.PendingCompletionDataKey]);

            await Task.WhenAll(tasks);

            // We don't know *exactly* how many are queued. It could actually be up to _maxParallel + _maxQueueLength if
            // no tasks have started executing yet. Probably don't need to continue asserting this. There may be some other
            // assertion we can replace it with, though.
            //Assert.Equal(maxQueueLength, e.Data[QueueLengthExceededException.CurrentlyQueuedDataKey]);
        }

        private static QueuedTaskSchedulerIsolationStrategy CreateIsolation(int maxConcurrency, int maxQueueLength)
        {
            return new QueuedTaskSchedulerIsolationStrategy(new TransientConfigurableValue<int>(maxConcurrency), new TransientConfigurableValue<int>(maxQueueLength));
        }

        // TODO Sleeping is janky. It'd be better to wire in a TaskCompletionSource and control their lifecycle that way.
        // - It's a bit difficult with the way I've wired the Scheduler and Factory together.
        // - Also, since QueueTask() is internal/protected, I can't just call it from within the isolation strategy.
        private static object SleepOneSecond()
        {
            Thread.Sleep(TimeSpan.FromSeconds(1));
            LogTime("Finished");
            return new { };
        }

        private static object ReturnImmediately()
        {
            return new { };
        }

        private static void LogTime(string message)
        {
            Debug.WriteLine(new TimeSpan(DateTime.UtcNow.Ticks).TotalMilliseconds + " - " + message);
        }

        // TODO Also test with some objects that:
        // - Throw exceptions immediately
        // - Throw exceptions from an async/await call
        // - Throw exceptions from async when not awaited
        // - Return values from an awaited async call
        // - Return an async Task that's not awaited
        // - Tasks (async and/or awaited) that get canceled via CancellationToken.
        // - TaskScheduler throws an exception that's not a QueueLengthExceededException
        // - Null tasks
    }
}
