using Hudl.Mjolnir.Bulkhead;
using Hudl.Mjolnir.Tests.Helper;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Hudl.Mjolnir.Tests.Bulkhead
{
    public class SemaphoreBulkheadTests : TestFixture
    {
        [Fact]
        public void Constructor_ThrowsIfMaxConcurrentNegative()
        {
            // Arrange

            const int invalidMaxConcurrent = -1;

            // Act + Assert

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new SemaphoreBulkhead(AnyGroupKey, invalidMaxConcurrent));
            Assert.Equal("maxConcurrent", exception.ParamName);
            Assert.Equal(invalidMaxConcurrent, exception.ActualValue);
        }

        [Fact]
        public void Name_IsKeyName()
        {
            // Arrange

            var key = AnyGroupKey;
            var bulkhead = new SemaphoreBulkhead(key, AnyPositiveInt);

            // Assert

            Assert.Equal(key.Name, bulkhead.Name);
        }

        [Fact]
        public void CountAvailable_IncreasesOnTryEnterAndDecreasesOnRelease()
        {
            // Arrange

            const int maxConcurrent = 10;
            var bulkhead = new SemaphoreBulkhead(AnyGroupKey, maxConcurrent);

            // Act + Assert

            Assert.Equal(maxConcurrent, bulkhead.CountAvailable); // Initial count should = max concurrent

            // Enter the bulkhead once.
            Assert.True(bulkhead.TryEnter()); // True = we should have enough available to acquire semaphore
            Assert.Equal(maxConcurrent - 1, bulkhead.CountAvailable);

            // Now leave the bulkhead.
            bulkhead.Release();
            Assert.Equal(maxConcurrent, bulkhead.CountAvailable);
        }

        [Fact]
        public void TryEnter_WhenAtMaximum_ReturnsFalseAndDoesntTakeAnotherSemaphoreLock()
        {
            // Arrange

            const int maxConcurrent = 1;
            var bulkhead = new SemaphoreBulkhead(AnyGroupKey, maxConcurrent);

            // Act + Assert

            Assert.True(bulkhead.TryEnter()); // The first one should be allowed, we have one spot.
            Assert.Equal(0, bulkhead.CountAvailable);

            Assert.False(bulkhead.TryEnter()); // The second should be at capacity and get rejected.
            Assert.Equal(0, bulkhead.CountAvailable); // The count should still be at 0 and not go negative.

            // Cleanup

            bulkhead.Release();
        }
    }
}
