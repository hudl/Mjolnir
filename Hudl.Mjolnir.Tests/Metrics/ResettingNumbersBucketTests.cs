using System;
using System.Threading;
using System.Threading.Tasks;
using Hudl.Common.Clock;
using Hudl.Config;
using Hudl.Mjolnir.Command;
using Hudl.Mjolnir.Metrics;
using Hudl.Mjolnir.Tests.Helper;
using Hudl.Mjolnir.Tests.TestCommands;
using Hudl.Riemann;
using Xunit;

namespace Hudl.Mjolnir.Tests.Metrics
{
    public class ResettingNumbersBucketTests : TestFixture
    {
        [Fact]
        public void Construct_StartsWithZeroMetrics()
        {
            var bucket = CreateBucket();

            Assert.Equal(0, bucket.GetCount(CounterMetric.CommandSuccess));
            Assert.Equal(0, bucket.GetCount(CounterMetric.CommandFailure));
        }

        [Fact]
        public void Increment_Increments()
        {
            var bucket = CreateBucket();

            bucket.Increment(CounterMetric.CommandSuccess);
            Assert.Equal(1, bucket.GetCount(CounterMetric.CommandSuccess));
        }

        [Fact]
        public void Increment_DoesntIncrementOtherMetrics()
        {
            var bucket = CreateBucket();

            bucket.Increment(CounterMetric.CommandSuccess);
            Assert.Equal(0, bucket.GetCount(CounterMetric.CommandFailure));
        }

        [Fact]
        public void Increment_AfterPeriodExceeded_ResetsBeforeIncrementing()
        {
            const long periodMillis = 1000;
            var clock = new ManualTestClock();
            var bucket = new ResettingNumbersBucket(clock, new TransientConfigurableValue<long>(periodMillis));

            bucket.Increment(CounterMetric.CommandSuccess);

            clock.AddMilliseconds(periodMillis + 1);
            Assert.Equal(1, bucket.GetCount(CounterMetric.CommandSuccess));

            bucket.Increment(CounterMetric.CommandSuccess); // Should reset and then count one.
            Assert.Equal(1, bucket.GetCount(CounterMetric.CommandSuccess)); // Should be 1, not 2.
        }

        //[Fact]
        public async Task RunForABit()
        {
            var value = new { };
            var now = DateTime.UtcNow;
            var random = new Random();

            // NOTE: Don't forget to configure the host and port in cluster.config:
            //   Host: ec2-54-196-35-45.compute-1.amazonaws.com
            //   Port: 5555
            RiemannStats.Instance.Hostname = "ROB-DESKTOP.agilesports.local";

            while (true)
            {
                if (DateTime.UtcNow > now + TimeSpan.FromMinutes(30))
                {
                    break;
                }

                var command = new SuccessfulEchoCommandWithFallback(value)
                //var command = new TimingOutWithoutFallbackCommand(TimeSpan.FromMilliseconds(500))
                //var command = new FaultingTaskWithSleepingFallbackCommand(TimeSpan.FromMilliseconds(500))
                //var command = new ReallySleepyCommand
                {
                    ThreadPool = null,
                    CircuitBreaker = null,
                    FallbackSemaphore = null,
                    Riemann = RiemannStats.Instance,
                };

                try
                {
                    await command.InvokeAsync();
                }
                catch (Exception)
                {
                    // Expected.
                }
                

                Thread.Sleep(random.Next(10, 50));
            }
        }

        private ResettingNumbersBucket CreateBucket()
        {
            var clock = new ManualTestClock();
            return new ResettingNumbersBucket(clock, new TransientConfigurableValue<long>(10000));
        }

        private class ReallySleepyCommand : BaseTestCommand<object>
        {
            protected override Task<object> ExecuteAsync(CancellationToken cancellationToken)
            {
                Thread.Sleep(300);
                throw new ExpectedTestException("Poo");
            }

            protected override object Fallback(CommandFailedException instigator)
            {
                Thread.Sleep(150);
                return new { };
            }
        }
    }
}
