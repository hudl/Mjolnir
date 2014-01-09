using System;
using System.Collections.Generic;
using System.Linq;
using Hudl.Common.Clock;
using Hudl.Config;
using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Metrics;
using Hudl.Riemann;
using Moq;
using Xunit;

namespace Hudl.Mjolnir.Tests.Breaker
{
    public class FailurePercentageCircuitBreakerTests
    {
        /// <summary>
        /// The breaker should be created in a good state (fixed).
        /// </summary>
        [Fact]
        public void Construct_BreakerIsntTripped()
        {
            var breaker = new BreakerBuilder(10, 50).Create();
            Assert.True(breaker.IsAllowing());
        }
        
        /// <summary>
        /// MarkSuccess() should only clear the Tripped state and Reset() metrics
        /// if the breaker was previously tripped. If not, Reset() shouldn't be called.
        /// </summary>
        [Fact]
        public void MarkSuccess_WhenNotTripped_DoesntResetMetrics()
        {
            var mockMetrics = CreateMockMetricsWithSnapshot(0, 0);
            var breaker = new BreakerBuilder(10, 50).WithMockMetrics(mockMetrics).Create();

            Assert.True(breaker.IsAllowing()); // Shouldn't be tripped.

            breaker.MarkSuccess(0);
            mockMetrics.Verify(m => m.Reset(), Times.Never);
        }

        [Fact]
        public void MarkSuccess_WhenTrippedAndAfterWaitDuration_ResetsMetrics()
        {
            var clock = new ManualTestClock();
            var mockMetrics = CreateMockMetricsWithSnapshot(1, 100);
            var breaker = new BreakerBuilder(1, 50)
                .WithMockMetrics(mockMetrics)
                .WithWaitMillis(10000)
                .WithClock(clock)
                .Create();

            Assert.False(breaker.IsAllowing()); // Trip the breaker first.

            clock.AddMilliseconds(11000);

            Assert.True(breaker.IsAllowing()); // Single test...
            breaker.MarkSuccess(0); // ... that's successful.
            mockMetrics.Verify(m => m.Reset(), Times.Once);
        }

        [Fact]
        public void MarkSuccess_ImmediatelyAfterTrippingButStartedBeforeTripped_DoesntImmediatelyFix()
        {
            // 1. Breaker is near tripping.
            // 2. Operation A and B are Allowed and begin work.
            // 3. Before Operation A completes
            //    a. Operation B has an error and updates metrics.
            //    b. Operation C calls IsAllowing(), which trips breaker.
            // 4. Operation A completes successfully and calls MarkSuccess().
            // 5. Since breaker is tripped but we haven't hit our wait duration yet,
            //    MarkSuccess() should result in the the breaker remaining tripped.

            var clock = new ManualTestClock();
            var mockMetrics = CreateMockMetricsWithSnapshot(2, 100); // 2 ops, 100% failing.
            var breaker = new BreakerBuilder(5, 1)
                .WithMockMetrics(mockMetrics)
                .WithClock(clock)
                .Create(); // 5 ops, 1% failure required to break.
            
            // #2. Operation A is allowed and begins.
            Assert.True(breaker.IsAllowing()); // Haven't hit the 1-operation threshold yet, should be allowed.
             
            // #3a. Operation B errors
            breaker.Properties.MinimumOperations.Value = 1; // Easier to test by changing the breaker conditions.

            // #3b. Breaker exceeds metrics thresholds, Operation C tries to IsAllowing() and trips breaker.
            Assert.False(breaker.IsAllowing());

            // #4. Operation A completes successfully.
            // Breaker's internal _lastTrippedTimestamp should be equal to zero (current clock time).
            // Since we say the transaction took 100ms, that'll be before the breaker tripped, and should
            // be ignored.
            breaker.MarkSuccess(100);

            // #5. Make sure we're still tripped and we didn't reset the metrics.
            Assert.False(breaker.IsAllowing());
            mockMetrics.Verify(m => m.Reset(), Times.Never);
        }

        [Fact]
        public void MarkSuccess_ForLongRunningSingleTest_FixesBreaker()
        {
            // Since we use the elapsed time in MarkSuccess(), address the following situation:
            // 1. Breaker trips
            // 2. Window passes
            // 3. Single test is allowed
            // 4. Single test takes a long time (say, > 2 windows) to complete (even though it started after we tripped).
            // Verify that:
            // - No other requests are allowed while the single test is waiting.
            // - The single test, if successful, fixes the breaker.

            // This is somewhat unlikely since we probably wont have command timeouts
            // that'd allow this. But worth verifying.

            var clock = new ManualTestClock();
            var mockMetrics = CreateMockMetricsWithSnapshot(2, 100); // 2 ops, 100% failing.
            var breaker = new BreakerBuilder(1, 1)
                .WithMockMetrics(mockMetrics)
                .WithClock(clock)
                .Create(); // 1 ops, 1% failure required to break.

            var duration = breaker.Properties.TrippedDurationMillis;

            Assert.False(breaker.IsAllowing()); // Should immediately trip.

            clock.AddMilliseconds(duration.Value + 10);

            Assert.True(breaker.IsAllowing()); // Single test is allowed.
            Assert.False(breaker.IsAllowing());

            clock.AddMilliseconds(duration.Value * 2);
            breaker.MarkSuccess(duration.Value * 2);

            // Metrics will have been reset.
            mockMetrics.Setup(m => m.GetSnapshot()).Returns(new MetricsSnapshot(0, 0));

            Assert.True(breaker.IsAllowing());
            Assert.True(breaker.IsAllowing());
        }

        [Fact]
        public void IsAllowing_WhenPropertiesForceTripped_Rejects()
        {
            // Most property values don't matter, IsAllowing() should reject before it tries to use them.
            var breaker = new BreakerBuilder(0, 0).Create();
            breaker.Properties.ForceTripped.Value = true;

            Assert.False(breaker.IsAllowing());
        }

        [Fact]
        public void IsAllowing_WhenPropertiesForceFixed_Allows()
        {
            var breaker = new BreakerBuilder(0, 0).Create();
            breaker.Properties.ForceFixed.Value = true;

            Assert.True(breaker.IsAllowing());
        }

        [Fact]
        public void IsAllowing_WhenBothForcePropertiesSet_Rejects()
        {
            var breaker = new BreakerBuilder(0, 0).Create();
            breaker.Properties.ForceTripped.Value = true;
            breaker.Properties.ForceFixed.Value = true;

            Assert.False(breaker.IsAllowing());
        }

        [Fact]
        public void IsAllowing_WhenPropertiesForceFixedButBreakerWouldNormallyTrip_SilentlyTripsTheBreaker()
        {
            var mockMetrics = CreateMockMetricsWithSnapshot(2, 50);
            var breaker = new BreakerBuilder(1, 25).WithMockMetrics(mockMetrics).Create();
            breaker.Properties.ForceFixed.Value = true;

            Assert.True(breaker.IsAllowing()); // Will have tripped internally.
            Assert.True(breaker.IsAllowing()); // Continues to allow even when tripped.

            // Test that if we remove the forced fix property, IsAllowing() then rejects.

            breaker.Properties.ForceFixed.Value = false;
            Assert.False(breaker.IsAllowing());
        }

        [Fact]
        public void IsAllowing_AfterTrippedAndWithinWaitPeriod_Rejects()
        {
            var clock = new ManualTestClock();
            var mockMetrics = CreateMockMetricsWithSnapshot(10, 100); // 10 ops, 100% failing.
            var breaker = new BreakerBuilder(1, 1) // Trip at 1 op, 1% failing.
                .WithMockMetrics(mockMetrics)
                .WithWaitMillis(10000)
                .WithClock(clock)
                .Create();

            Assert.False(breaker.IsAllowing()); // Should reject and trip.

            clock.AddMilliseconds(5000); // Half the wait duration.

            Assert.False(breaker.IsAllowing());
        }

        [Fact]
        public void IsAllowing_AfterTrippedAndAfterWaitPeriod_SendsSingleTestAndRejectsOthers()
        {
            var clock = new ManualTestClock();
            var mockMetrics = CreateMockMetricsWithSnapshot(10, 100); // 10 ops, 100% failing.
            var breaker = new BreakerBuilder(1, 1) // Trip at 1 op, 1% failing.
                .WithMockMetrics(mockMetrics)
                .WithWaitMillis(10000)
                .WithClock(clock)
                .Create();

            Assert.False(breaker.IsAllowing()); // Should reject and trip.

            clock.AddMilliseconds(11000);

            Assert.True(breaker.IsAllowing()); // Allow the first one, it's the single test.
            Assert.False(breaker.IsAllowing()); // Reject the next one, the test was already allowed.
        }

        [Fact]
        public void IsAllowing_AfterSuccessfulSingleTest_FixesBreaker()
        {
            var clock = new ManualTestClock();
            var mockMetrics = CreateMockMetricsWithSnapshot(10, 100); // 10 ops, 100% failing.
            var breaker = new BreakerBuilder(1, 1) // Trip at 1 op, 1% failing.
                .WithMockMetrics(mockMetrics)
                .WithWaitMillis(10000)
                .WithClock(clock)
                .Create();

            Assert.False(breaker.IsAllowing()); // Should reject and trip.
            
            clock.AddMilliseconds(11000);
            breaker.IsAllowing(); // Send the single test.

            clock.AddMilliseconds(500); // Pretend the test operation took a bit (not really necessary here).
            breaker.MarkSuccess(0); // The single test transaction marks a success.

            // Metrics should be reset.
            mockMetrics.Verify(m => m.Reset(), Times.Once);
            mockMetrics.Setup(m => m.GetSnapshot()).Returns(new MetricsSnapshot(0, 0));

            // Should be fixed now.
            // Test IsAllowing() twice to make sure it's not just the single test we're allowing.
            Assert.True(breaker.IsAllowing());
            Assert.True(breaker.IsAllowing());
        }

        [Fact]
        public void IsAllowing_AfterFailedSingleTest_KeepsBreakerTrippedAndSendsAnotherTestAfterAnotherDuration()
        {
            var clock = new ManualTestClock();
            var mockMetrics = CreateMockMetricsWithSnapshot(10, 100); // 10 ops, 100% failing.
            var breaker = new BreakerBuilder(1, 1) // Trip at 1 op, 1% failing.
                .WithMockMetrics(mockMetrics)
                .WithWaitMillis(10000)
                .WithClock(clock)
                .Create();

            Assert.False(breaker.IsAllowing()); // Should reject and trip.

            clock.AddMilliseconds(11000);
            Assert.True(breaker.IsAllowing()); // Single test.

            // Advance to halfway through the duration. We should still be rejecting.
            clock.AddMilliseconds(6000);
            Assert.False(breaker.IsAllowing());

            // No mark success call here. Metrics will probably have a failure added to it.

            clock.AddMilliseconds(5000); // 6000 + 5000 = 11000 > 10000 (the duration).
            Assert.True(breaker.IsAllowing()); // It's been another duration, we should allow another single test.

            // Advance to halfway through the duration. We should still be rejecting.
            clock.AddMilliseconds(6000);
            Assert.False(breaker.IsAllowing());

            // No mark success call here. Metrics will probably have a failure added to it.

            clock.AddMilliseconds(5000); // 6000 + 5000 = 11000 > 10000 (the duration).
            Assert.True(breaker.IsAllowing()); // It's been another duration, we should allow another single test.

            // Advance a second or so, pretend the single test took some time.
            clock.AddMilliseconds(500);

            // Let's pretend this test succeeds. Mark it and reset the metrics.
            breaker.MarkSuccess(0);
            mockMetrics.Setup(m => m.GetSnapshot()).Returns(new MetricsSnapshot(0, 0));

            // We should immediately be opened.
            Assert.True(breaker.IsAllowing());
            Assert.True(breaker.IsAllowing());
            Assert.True(breaker.IsAllowing());
        }

        [Fact]
        public void IsAllowing_MultipleDurationsBetweenFailureAndNextSuccess_FixesBreakerOnSuccess()
        {
            const long durationMillis = 10000;

            var clock = new ManualTestClock();
            var mockMetrics = CreateMockMetricsWithSnapshot(10, 100); // 10 ops, 100% failing.
            var breaker = new BreakerBuilder(1, 1) // Trip at 1 op, 1% failing.
                .WithMockMetrics(mockMetrics)
                .WithWaitMillis(durationMillis)
                .WithClock(clock)
                .Create();

            Assert.False(breaker.IsAllowing()); // Trip the breaker.

            clock.AddMilliseconds(durationMillis * 5);

            Assert.True(breaker.IsAllowing()); // Single test.
            breaker.MarkSuccess(0);
            mockMetrics.Setup(m => m.GetSnapshot()).Returns(new MetricsSnapshot(0, 0));

            Assert.True(breaker.IsAllowing());
            Assert.True(breaker.IsAllowing());
            Assert.True(breaker.IsAllowing());
        }

        [Fact]
        public void IsAllowing_MultipleDurationsBetweenFailureAndNextFailure_KeepsBreakerTripped()
        {
            const long durationMillis = 10000;

            var clock = new ManualTestClock();
            var mockMetrics = CreateMockMetricsWithSnapshot(10, 100); // 10 ops, 100% failing.
            var breaker = new BreakerBuilder(1, 1) // Trip at 1 op, 1% failing.
                .WithMockMetrics(mockMetrics)
                .WithWaitMillis(durationMillis)
                .WithClock(clock)
                .Create();

            Assert.False(breaker.IsAllowing()); // Trip the breaker.

            clock.AddMilliseconds(durationMillis * 5);

            Assert.True(breaker.IsAllowing()); // Single test, but it won't MarkSuccess().
            Assert.False(breaker.IsAllowing()); // Make sure the last one was the single test.

            clock.AddMilliseconds(durationMillis * 5);

            Assert.True(breaker.IsAllowing()); // Single test, but it won't MarkSuccess().
            Assert.False(breaker.IsAllowing()); // Make sure the last one was the single test.
        }

        [Fact]
        public void IsAllowing_WhenAlreadyTripped_DoesntReTripBreaker()
        {
            const long durationMillis = 10000;

            var mockMetrics = CreateMockMetricsWithSnapshot(10, 100); // 10 ops, 100% failing.
            var riemann = new InternallyCountingRiemann();
            var breaker = new BreakerBuilder(1, 1, "Test") // Trip at 1 op, 1% failing.
                .WithMockMetrics(mockMetrics)
                .WithWaitMillis(durationMillis)
                .WithRiemann(riemann)
                .Create();
            breaker.IsAllowing(); // Trip the breaker.
            Assert.Equal(1, riemann.ServicesAndStates.Count(ss => ss.Service == "mjolnir breaker Test" && ss.State == "Tripped"));

            breaker.IsAllowing(); // Make another call, which should bail immediately (and not re-trip).

            // Best way to test this right now is to make sure we don't fire a stat for the state change.
            Assert.Equal(1, riemann.ServicesAndStates.Count(ss => ss.Service == "mjolnir breaker Test" && ss.State == "Tripped"));
        }

        // The following tests compare the metrics to the threshold. The names have been made more concise.
        // For example, "AboveTotalBelowThreshold" means:
        //   - The current Metrics total count is above the configured Breaker count
        //   - The current Metrics error percent is below the configured Breaker percent

        [Fact]
        public void IsAllowing_BelowTotalBelowThreshold_AllowsAndDoesntTrip()
        {
            TripTest.WithMetricsAt(2, 50).WithBreakerAt(3, 75).ShouldTrip(false).Run();
        }

        [Fact]
        public void IsAllowing_BelowTotalEqualThreshold_AllowsAndDoesntTrip()
        {
            TripTest.WithMetricsAt(2, 50).WithBreakerAt(3, 50).ShouldTrip(false).Run();
        }

        [Fact]
        public void IsAllowing_BelowTotalAboveThreshold_AllowsAndDoesntTrip()
        {
            TripTest.WithMetricsAt(2, 50).WithBreakerAt(3, 25).ShouldTrip(false).Run();
        }

        [Fact]
        public void IsAllowing_EqualTotalBelowThreshold_AllowsAndDoesntTrip()
        {
            TripTest.WithMetricsAt(2, 50).WithBreakerAt(2, 75).ShouldTrip(false).Run();
        }

        [Fact]
        public void IsAllowing_EqualTotalEqualThreshold_RejectsAndTrips()
        {
            TripTest.WithMetricsAt(2, 50).WithBreakerAt(2, 50).ShouldTrip(true).Run();
        }

        [Fact]
        public void IsAllowing_EqualTotalAboveThreshold_RejectsAndTrips()
        {
            TripTest.WithMetricsAt(2, 50).WithBreakerAt(2, 25).ShouldTrip(true).Run();
        }

        [Fact]
        public void IsAllowing_AboveTotalBelowThreshold_AllowsAndDoesntTrip()
        {
            TripTest.WithMetricsAt(2, 50).WithBreakerAt(1, 75).ShouldTrip(false).Run();
        }

        [Fact]
        public void IsAllowing_AboveTotalEqualThreshold_RejectsAndTrips()
        {
            TripTest.WithMetricsAt(2, 50).WithBreakerAt(1, 50).ShouldTrip(true).Run();
        }

        [Fact]
        public void IsAllowing_AboveTotalAboveThreshold_RejectsAndTrips()
        {
            TripTest.WithMetricsAt(2, 50).WithBreakerAt(1, 25).ShouldTrip(true).Run();
        }

        // Helper for testing the breaker with different total/threshold combinations.
        public class TripTest
        {
            private TripTest() {}

            private long _metricsTotal;
            private int _metricsPercent;
            private long _breakerTotal;
            private int _breakerPercent;
            private bool _shouldTrip;

            public static TripTest WithMetricsAt(long total, int errorPercent)
            {
                return new TripTest
                {
                    _metricsTotal = total,
                    _metricsPercent = errorPercent,
                };
            }

            public TripTest WithBreakerAt(long total, int thresholdPercent)
            {
                _breakerTotal = total;
                _breakerPercent = thresholdPercent;
                return this;
            }

            public TripTest ShouldTrip(bool shouldTrip)
            {
                _shouldTrip = shouldTrip;
                return this;
            }

            public void Run()
            {
                var mockMetrics = CreateMockMetricsWithSnapshot(_metricsTotal, _metricsPercent);
                var properties = CreateBreakerProperties(_breakerTotal, _breakerPercent, 30000);
                var breaker = new FailurePercentageCircuitBreaker(GroupKey.Named("Test"), mockMetrics.Object, properties);

                Assert.NotEqual(_shouldTrip, breaker.IsAllowing());
            }
        }

        /// <summary>
        /// Creates a mock metrics object whose GetSnapshot() will have the provided current total and error percent.
        /// </summary>
        internal static Mock<ICommandMetrics> CreateMockMetricsWithSnapshot(long total, int percent)
        {
            var mockMetrics = new Mock<ICommandMetrics>();
            mockMetrics.Setup(m => m.GetSnapshot()).Returns(new MetricsSnapshot(total, percent));
            return mockMetrics;
        }

        internal static FailurePercentageCircuitBreakerProperties CreateBreakerProperties(long minimumOperations, int thresholdPercentage, long brokenDurationMillis, bool forceTripped = false, bool forceFixed = false)
        {
            return new FailurePercentageCircuitBreakerProperties(
                new TransientConfigurableValue<long>(minimumOperations),
                new TransientConfigurableValue<int>(thresholdPercentage),
                new TransientConfigurableValue<long>(brokenDurationMillis),
                new TransientConfigurableValue<bool>(forceTripped),
                new TransientConfigurableValue<bool>(forceFixed));
        }
    }

    internal class BreakerBuilder
    {
        private readonly long _minimumOperations;
        private readonly int _failurePercent;
        private readonly string _key;

        private long _waitMillis = 30000;
        private IClock _clock = new SystemClock();
        private IMock<ICommandMetrics> _mockMetrics = FailurePercentageCircuitBreakerTests.CreateMockMetricsWithSnapshot(0, 0);
        private IRiemann _riemann = new Mock<IRiemann>().Object;

        public BreakerBuilder(long minimumOperations, int failurePercent, string key = null)
        {
            _minimumOperations = minimumOperations;
            _failurePercent = failurePercent;
            _key = key ?? "Test";
        }

        public BreakerBuilder WithMockMetrics(IMock<ICommandMetrics> mockMetrics)
        {
            _mockMetrics = mockMetrics;
            return this;
        }

        public BreakerBuilder WithWaitMillis(long waitMillis)
        {
            _waitMillis = waitMillis;
            return this;
        }

        public BreakerBuilder WithClock(IClock clock)
        {
            _clock = clock;
            return this;
        }

        public BreakerBuilder WithRiemann(IRiemann riemann)
        {
            _riemann = riemann;
            return this;
        }

        public FailurePercentageCircuitBreaker Create()
        {
            var properties = FailurePercentageCircuitBreakerTests.CreateBreakerProperties(_minimumOperations, _failurePercent, _waitMillis);
            return new FailurePercentageCircuitBreaker(GroupKey.Named(_key), _clock, _mockMetrics.Object, _riemann, properties);
        }
    }

    internal class InternallyCountingRiemann : IRiemann
    {
        public List<ServiceAndState> ServicesAndStates { get; set; }

        public InternallyCountingRiemann()
        {
             ServicesAndStates = new List<ServiceAndState>(); 
        }

        public void Event(string service, string state, long? metric = null, ISet<string> tags = null, string description = null, int? ttl = null)
        {
            ServicesAndStates.Add(new ServiceAndState { Service = service, State = state });
        }

        public void Event(string service, string state, float? metric = null, ISet<string> tags = null, string description = null, int? ttl = null)
        {
            ServicesAndStates.Add(new ServiceAndState { Service = service, State = state });
        }

        public void Event(string service, string state, double? metric = null, ISet<string> tags = null, string description = null, int? ttl = null)
        {
            ServicesAndStates.Add(new ServiceAndState { Service = service, State = state });
        }

        public void Elapsed(string service, string state, TimeSpan elapsed, ISet<string> tags = null, string description = null, int? ttl = null)
        {
            ServicesAndStates.Add(new ServiceAndState { Service = service, State = state });
        }

        public void Gauge(string service, string state, long? metric = null, ISet<string> tags = null, string description = null, int? ttl = null)
        {
            ServicesAndStates.Add(new ServiceAndState { Service = service, State = state });
        }

        public void ConfigGauge(string service, long metric)
        {
            // Ignored.
        }

        public class ServiceAndState
        {
            public string Service { get; set; }
            public string State { get; set; }
        }
    }
}
