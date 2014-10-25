using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Hudl.Config;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Isolation;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Tests.Helper;
using Moq;
using Xunit;

namespace Hudl.Mjolnir.Tests.Isolation
{
    public class TaskSchedulerQueuedIsolationTests
    {
        [Fact]
        public void Construct_MaxConcurrency_ArgumentValidation()
        {
            var maxQueueLength = new TransientConfigurableValue<int>(1); // Good value for queue length, not testing it here.

            Assert.Throws<ArgumentOutOfRangeException>(() => new QueuedTaskSchedulerIsolationStrategy(GroupKey.Named("foo"), new TransientConfigurableValue<int>(0), maxQueueLength, new Mock<IStats>().Object));
            Assert.Throws<ArgumentOutOfRangeException>(() => new QueuedTaskSchedulerIsolationStrategy(GroupKey.Named("foo"), new TransientConfigurableValue<int>(-1), maxQueueLength, new Mock<IStats>().Object));
            Assert.Throws<ArgumentOutOfRangeException>(() => new QueuedTaskSchedulerIsolationStrategy(GroupKey.Named("foo"), new TransientConfigurableValue<int>(Int32.MinValue), maxQueueLength, new Mock<IStats>().Object));
            Assert.DoesNotThrow(() => new QueuedTaskSchedulerIsolationStrategy(GroupKey.Named("foo"), new TransientConfigurableValue<int>(1), maxQueueLength, new Mock<IStats>().Object));
            Assert.DoesNotThrow(() => new QueuedTaskSchedulerIsolationStrategy(GroupKey.Named("foo"), new TransientConfigurableValue<int>(Int32.MaxValue), maxQueueLength, new Mock<IStats>().Object));
        }

        [Fact]
        public void Construct_MaxQueueLength_ArgumentValidation()
        {
            var maxConcurrency = new TransientConfigurableValue<int>(1); // Good value for max concurrency, not testing it here.

            Assert.Throws<ArgumentOutOfRangeException>(() => new QueuedTaskSchedulerIsolationStrategy(GroupKey.Named("foo"), maxConcurrency, new TransientConfigurableValue<int>(-1), new Mock<IStats>().Object));
            Assert.Throws<ArgumentOutOfRangeException>(() => new QueuedTaskSchedulerIsolationStrategy(GroupKey.Named("foo"), maxConcurrency, new TransientConfigurableValue<int>(Int32.MinValue), new Mock<IStats>().Object));
            Assert.DoesNotThrow(() => new QueuedTaskSchedulerIsolationStrategy(GroupKey.Named("foo"), maxConcurrency, new TransientConfigurableValue<int>(0), new Mock<IStats>().Object));
            Assert.DoesNotThrow(() => new QueuedTaskSchedulerIsolationStrategy(GroupKey.Named("foo"), maxConcurrency, new TransientConfigurableValue<int>(1), new Mock<IStats>().Object));
            Assert.DoesNotThrow(() => new QueuedTaskSchedulerIsolationStrategy(GroupKey.Named("foo"), maxConcurrency, new TransientConfigurableValue<int>(Int32.MaxValue), new Mock<IStats>().Object));
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
            Assert.DoesNotThrow(() => isolation.Enqueue<object>(ReturnImmediately, CancellationToken.None));
            Thread.Sleep(10); // Make sure it completes (by hacky-sleeping and hoping that's enough).
            Assert.DoesNotThrow(() => isolation.Enqueue<object>(ReturnImmediately, CancellationToken.None));
        }

        // TODO Also test:
        // - Delayed exception
        // - Throw exceptions from async when not awaited
        // - Return values from an awaited async call
        // - Return an async Task that's not awaited
        // - Tasks (async and/or awaited) that get canceled via CancellationToken.
        // - TaskScheduler throws an exception that's not a QueueLengthExceededException
        // - Null tasks
        // Test these when the Enqueue both is and is not awaited.

        [Fact]
        public async Task Enqueue_ImmediateSyncException_Awaited()
        {
            // When awaited, the enqueued task should throw its exception.

            var expected = new ExpectedTestException("Expected");
            var isolation = CreateIsolation(10, 10); // Constraints don't matter.

            try
            {
                var task = isolation.Enqueue<object>(() =>
                {
                    throw expected;
                }, CancellationToken.None);
                await task;
            }
            catch (ExpectedTestException e)
            {
                Assert.Equal(expected, e);
            }
        }

        [Fact]
        public async Task Enqueue_ImmediateSyncException_Continuation()
        {
            // If the task is assigned a continuation and not immediately awaited,
            // the task should be Faulted and contain the exception thrown from within.

            var expected = new ExpectedTestException("Expected");
            var isolation = CreateIsolation(10, 10); // Constraints don't matter.
            var task = isolation.Enqueue<object>(() =>
            {
                throw expected;
            }, CancellationToken.None);

            await task.ContinueWith(res =>
            {
                Assert.True(res.IsFaulted);
                Assert.True(res.Exception != null && res.Exception.InnerException == expected);
            });
        }

        [Fact]
        public async Task Enqueue_RapidFireExceptions()
        {
            var isolation = CreateIsolation(50, 50);
            var tasks = new List<Task>();
            for (var i = 0; i < 100; i++)
            {
                var i0 = i;
                var task = isolation.Enqueue(() =>
                {
                    throw new ExpectedTestException("Expected " + i0);
                }, CancellationToken.None);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks).ContinueWith(res =>
            {
                Assert.True(tasks.TrueForAll(task => task.IsFaulted));
                Assert.True(tasks.TrueForAll(task => task.Exception.InnerException is ExpectedTestException));
            });
        }

        [Fact]
        public async Task Enqueue_OneThousandTasks()
        {
            var isolation = CreateIsolation(1000, 0);
            var tasks = new List<Task>();
            for (var i = 0; i < 1000; i++)
            {
                var task = isolation.Enqueue(() => Thread.Sleep(10), CancellationToken.None);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task Enqueue_ChildTasksDontAffectIsolationConcurrency()
        {
            var isolation = CreateIsolation(2, 0);
            var task = isolation.Enqueue(() => // First queued
            {
                // Kick off 10 tasks. None of them should use our isolation's custom scheduler,
                // so none of them should throw an exception;
                for (var i = 0; i < 10; i++)
                {
                    Task.Factory.StartNew(() => Thread.Sleep(10)); // Shouldn't be queued with isolation scheduler.
                }
            }, CancellationToken.None);

            // Since only one (the initial Enqueue()) should have been queued to our scheduler above,
            // we should have room for another.
            Assert.DoesNotThrow(() => isolation.Enqueue(() => true, CancellationToken.None));

            await task;
        }

        // TODO Consideration for attached/detached child tasks.

        // TODO Consideration for ThreadPool size. Should we actually be using the application pool, or instead manage our own?
        // - A risk point is pushing up on the application pool max (or current size). How fast does it scale?
        // - Get some metrics on current pool usage in production.

        private static async Task RunEnqueueTest(int maxConcurrency, int maxQueueLength)
        {
            LogTime("Started");
            var isolation = CreateIsolation(maxConcurrency, maxQueueLength);

            var tasks = new List<Task<object>>();
            for (var i = 0; i < maxConcurrency + maxQueueLength; i++)
            {
                Assert.DoesNotThrow(() => tasks.Add(isolation.Enqueue<object>(SleepOneSecond, CancellationToken.None)));
            }

            var e = Assert.Throws<IsolationStrategyRejectedException>(() => tasks.Add(isolation.Enqueue<object>(SleepOneSecond, CancellationToken.None)));

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
            return new QueuedTaskSchedulerIsolationStrategy(GroupKey.Named("foo"), new TransientConfigurableValue<int>(maxConcurrency), new TransientConfigurableValue<int>(maxQueueLength), new Mock<IStats>().Object);
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

        
    }
}
