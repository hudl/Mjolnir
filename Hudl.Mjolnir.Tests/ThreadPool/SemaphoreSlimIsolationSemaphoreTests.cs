using System.Diagnostics;
using Hudl.Config;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.ThreadPool;
using Xunit;

namespace Hudl.Mjolnir.Tests.ThreadPool
{
    public class SemaphoreSlimIsolationSemaphoreTests
    {
        [Fact]
        public void TryEnter_WhenSemaphoreIsAvailable_ReturnsTrueImmediately()
        {
            var semaphore = new SemaphoreSlimIsolationSemaphore(GroupKey.Named("Test"), new TransientConfigurableValue<int>(1));

            var stopwatch = Stopwatch.StartNew();
            Assert.True(semaphore.TryEnter());
            Assert.True(stopwatch.ElapsedMilliseconds < 10);
        }

        [Fact]
        public void TryEnter_WhenSemaphoreNotAvailable_ReturnsFalseImmediately()
        {
            var semaphore = new SemaphoreSlimIsolationSemaphore(GroupKey.Named("Test"), new TransientConfigurableValue<int>(1));
            semaphore.TryEnter();

            var stopwatch = Stopwatch.StartNew();
            Assert.False(semaphore.TryEnter());
            Assert.True(stopwatch.ElapsedMilliseconds < 10);
        }

        [Fact]
        public void Release_WhenSemaphoreNotAvailable_MakesItAvailable()
        {
            var semaphore = new SemaphoreSlimIsolationSemaphore(GroupKey.Named("Test"), new TransientConfigurableValue<int>(1));

            semaphore.TryEnter();
            Assert.False(semaphore.TryEnter());

            semaphore.Release();
            Assert.True(semaphore.TryEnter());
        }

        [Fact]
        public void Release_WhenSemaphoreNotInUse_DoesNothing()
        {
            var semaphore = new SemaphoreSlimIsolationSemaphore(GroupKey.Named("Test"), new TransientConfigurableValue<int>(5));
            semaphore.Release(); // Shouldn't throw.
        }

        [Fact]
        public void TryEnterAndReleaseALot()
        {
            var semaphore = new SemaphoreSlimIsolationSemaphore(GroupKey.Named("Test"), new TransientConfigurableValue<int>(5));

            Assert.True(semaphore.TryEnter());
            Assert.True(semaphore.TryEnter());
            Assert.True(semaphore.TryEnter());
            Assert.True(semaphore.TryEnter());
            Assert.True(semaphore.TryEnter());
            Assert.False(semaphore.TryEnter());
            Assert.False(semaphore.TryEnter());
            Assert.False(semaphore.TryEnter());
            semaphore.Release();
            Assert.True(semaphore.TryEnter());
            Assert.False(semaphore.TryEnter());
            semaphore.Release();
            semaphore.Release();
            semaphore.Release();
            semaphore.Release();
            semaphore.Release();
            Assert.True(semaphore.TryEnter());
            Assert.True(semaphore.TryEnter());
            Assert.True(semaphore.TryEnter());
            semaphore.Release();
            Assert.True(semaphore.TryEnter());
            Assert.True(semaphore.TryEnter());
            Assert.True(semaphore.TryEnter());
            Assert.False(semaphore.TryEnter());
            Assert.False(semaphore.TryEnter());
            semaphore.Release();
            semaphore.Release();
            semaphore.Release();
            semaphore.Release();
            semaphore.Release();
        }
    }
}
