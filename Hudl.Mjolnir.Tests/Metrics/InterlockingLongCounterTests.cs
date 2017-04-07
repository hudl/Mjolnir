using System.Collections.Generic;
using System.Threading.Tasks;
using Hudl.Mjolnir.Metrics;
using Hudl.Mjolnir.Tests.Helper;
using Xunit;

namespace Hudl.Mjolnir.Tests.Metrics
{
    public class InterlockingLongCounterTests : TestFixture
    {
        [Fact]
        public void Increment_Increments()
        {
            // Arrange

            var counter = new InterlockingLongCounter();

            // Act

            counter.Increment();

            // Assert

            Assert.Equal(1, counter.Get());
        }

        [Fact]
        public void Stress_Increment_RetainsAccuracy()
        {
            // Arrange

            const int iterations = 100000;
            var counter = new InterlockingLongCounter();
            var tasks = new List<Task>();

            // Act

            for (var i = 0; i < iterations; i++)
            {
                var task = Task.Run(() => counter.Increment());
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            // Assert

            Assert.Equal(iterations, counter.Get());
        }
    }
}
