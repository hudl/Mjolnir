using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.Clock;
using Hudl.Mjolnir.Events;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Log;
using Hudl.Mjolnir.Metrics;
using Hudl.Mjolnir.Tests.Helper;
using Moq;
using Xunit;

namespace Hudl.Mjolnir.Tests.Breaker
{
    public class FailurePercentageCircuitBreakerTests : TestFixture
    {
        private static readonly GroupKey AnyKey = GroupKey.Named(Rand.String());

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


            // Arrange
            
            var manualClock = new ManualTestClock();

            var mockMetrics = new Mock<ICommandMetrics>();
            mockMetrics.Setup(m => m.GetSnapshot()).Returns(new MetricsSnapshot(2, 100)); // 2 ops, 100% failing.

            var mockEvents = new Mock<IMetricEvents>();

            var mockConfig = new Mock<IFailurePercentageCircuitBreakerConfig>();
            mockConfig.SetupSequence(m => m.GetMinimumOperations(It.IsAny<GroupKey>()))
                .Returns(5) // First access should be > 1 so that the breaker doesn't trip.
                .Returns(1); // Second access should be 1, so that we trip the breaker because we have the minimum met.
            
            mockConfig.Setup(m => m.GetThresholdPercentage(It.IsAny<GroupKey>())).Returns(1);
            mockConfig.Setup(m => m.GetTrippedDurationMillis(It.IsAny<GroupKey>())).Returns(30000);
            mockConfig.Setup(m => m.GetForceTripped(It.IsAny<GroupKey>())).Returns(false);
            mockConfig.Setup(m => m.GetForceFixed(It.IsAny<GroupKey>())).Returns(false);
            
            // 5 ops, 1% failure required to break.
            var breaker = new FailurePercentageCircuitBreaker(AnyKey, manualClock, mockMetrics.Object, mockEvents.Object, mockConfig.Object, new DefaultMjolnirLogFactory());
            

            // Act / Assert

            // #2. Operation A is allowed and begins.
            Assert.True(breaker.IsAllowing()); // Haven't hit the 1-operation threshold yet, should be allowed.

            // #3a. Operation B errors. This is easier to simulate by changing the breaker's trip
            //      conditions. The sequenced Mock (above) has the second MinimumOperations returning
            //      1, which would now mean we have enough operations to trip (where we didn't before).
            
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


            // Arrange

            var manualClock = new ManualTestClock();

            var mockMetrics = new Mock<ICommandMetrics>();
            mockMetrics.Setup(m => m.GetSnapshot()).Returns(new MetricsSnapshot(2, 100)); // 2 ops, 100% failing.

            var mockEvents = new Mock<IMetricEvents>();

            const long trippedDurationMillis = 30000;
            var mockConfig = new Mock<IFailurePercentageCircuitBreakerConfig>();
            mockConfig.Setup(m => m.GetMinimumOperations(It.IsAny<GroupKey>())).Returns(1);
            mockConfig.Setup(m => m.GetThresholdPercentage(It.IsAny<GroupKey>())).Returns(1);
            mockConfig.Setup(m => m.GetTrippedDurationMillis(It.IsAny<GroupKey>())).Returns(trippedDurationMillis);
            mockConfig.Setup(m => m.GetForceTripped(It.IsAny<GroupKey>())).Returns(false);
            mockConfig.Setup(m => m.GetForceFixed(It.IsAny<GroupKey>())).Returns(false);
            
            // 1 ops, 1% failure required to break.
            var breaker = new FailurePercentageCircuitBreaker(AnyKey, manualClock, mockMetrics.Object, mockEvents.Object, mockConfig.Object, new DefaultMjolnirLogFactory());
            

            // Act / Assert
            
            Assert.False(breaker.IsAllowing()); // Should immediately trip.

            manualClock.AddMilliseconds(trippedDurationMillis + 10);
            
            Assert.True(breaker.IsAllowing()); // Single test is allowed.
            Assert.False(breaker.IsAllowing());

            manualClock.AddMilliseconds(trippedDurationMillis * 2);
            breaker.MarkSuccess(trippedDurationMillis * 2);

            // Metrics will have been reset.
            mockMetrics.Setup(m => m.GetSnapshot()).Returns(new MetricsSnapshot(0, 0));

            Assert.True(breaker.IsAllowing());
            Assert.True(breaker.IsAllowing());
        }

        [Fact]
        public void IsAllowing_WhenPropertiesForceTripped_Rejects()
        {
            // Arrange

            // Most property values don't matter, IsAllowing() should reject before it tries to use them.
            var manualClock = new ManualTestClock();

            var mockMetrics = new Mock<ICommandMetrics>();
            mockMetrics.Setup(m => m.GetSnapshot()).Returns(new MetricsSnapshot(0, 0));

            var mockEvents = new Mock<IMetricEvents>();

            var mockConfig = new Mock<IFailurePercentageCircuitBreakerConfig>();
            mockConfig.Setup(m => m.GetMinimumOperations(It.IsAny<GroupKey>())).Returns(1);
            mockConfig.Setup(m => m.GetThresholdPercentage(It.IsAny<GroupKey>())).Returns(1);
            mockConfig.Setup(m => m.GetTrippedDurationMillis(It.IsAny<GroupKey>())).Returns(30000);
            mockConfig.Setup(m => m.GetForceTripped(It.IsAny<GroupKey>())).Returns(true); // The config we're testing here.
            mockConfig.Setup(m => m.GetForceFixed(It.IsAny<GroupKey>())).Returns(false);
            
            var breaker = new FailurePercentageCircuitBreaker(AnyKey, manualClock, mockMetrics.Object, mockEvents.Object, mockConfig.Object, new DefaultMjolnirLogFactory());
            

            // Act / Assert

            Assert.False(breaker.IsAllowing());
        }

        [Fact]
        public void IsAllowing_WhenPropertiesForceFixed_Allows()
        {
            // Arrange

            // Most property values don't matter, IsAllowing() should reject before it tries to use them.
            var manualClock = new ManualTestClock();

            var mockMetrics = new Mock<ICommandMetrics>();
            mockMetrics.Setup(m => m.GetSnapshot()).Returns(new MetricsSnapshot(0, 0));

            var mockEvents = new Mock<IMetricEvents>();

            var mockConfig = new Mock<IFailurePercentageCircuitBreakerConfig>();
            mockConfig.Setup(m => m.GetMinimumOperations(It.IsAny<GroupKey>())).Returns(1);
            mockConfig.Setup(m => m.GetThresholdPercentage(It.IsAny<GroupKey>())).Returns(1);
            mockConfig.Setup(m => m.GetTrippedDurationMillis(It.IsAny<GroupKey>())).Returns(30000);
            mockConfig.Setup(m => m.GetForceTripped(It.IsAny<GroupKey>())).Returns(false);
            mockConfig.Setup(m => m.GetForceFixed(It.IsAny<GroupKey>())).Returns(true);  // The config we're testing here.
            
            var breaker = new FailurePercentageCircuitBreaker(AnyKey, manualClock, mockMetrics.Object, mockEvents.Object, mockConfig.Object, new DefaultMjolnirLogFactory());


            // Act / Assert

            Assert.True(breaker.IsAllowing());
        }

        [Fact]
        public void IsAllowing_WhenBothForcePropertiesSet_Rejects()
        {
            // Arrange

            // Most property values don't matter, IsAllowing() should reject before it tries to use them.
            var manualClock = new ManualTestClock();

            var mockMetrics = new Mock<ICommandMetrics>();
            mockMetrics.Setup(m => m.GetSnapshot()).Returns(new MetricsSnapshot(0, 0));

            var mockEvents = new Mock<IMetricEvents>();

            var mockConfig = new Mock<IFailurePercentageCircuitBreakerConfig>();
            mockConfig.Setup(m => m.GetMinimumOperations(It.IsAny<GroupKey>())).Returns(1);
            mockConfig.Setup(m => m.GetThresholdPercentage(It.IsAny<GroupKey>())).Returns(1);
            mockConfig.Setup(m => m.GetTrippedDurationMillis(It.IsAny<GroupKey>())).Returns(30000);
            mockConfig.Setup(m => m.GetForceTripped(It.IsAny<GroupKey>())).Returns(true); // The config we're testing here.
            mockConfig.Setup(m => m.GetForceFixed(It.IsAny<GroupKey>())).Returns(true); // The config we're testing here.
            
            var breaker = new FailurePercentageCircuitBreaker(AnyKey, manualClock, mockMetrics.Object, mockEvents.Object, mockConfig.Object, new DefaultMjolnirLogFactory());


            // Act / Assert

            Assert.False(breaker.IsAllowing());
        }

        [Fact]
        public void IsAllowing_WhenPropertiesForceFixedButBreakerWouldNormallyTrip_SilentlyTripsTheBreaker()
        {
            // Arrange

            // Most property values don't matter, IsAllowing() should reject before it tries to use them.
            var manualClock = new ManualTestClock();

            var mockMetrics = new Mock<ICommandMetrics>();
            mockMetrics.Setup(m => m.GetSnapshot()).Returns(new MetricsSnapshot(2, 50));

            var mockEvents = new Mock<IMetricEvents>();

            var mockConfig = new Mock<IFailurePercentageCircuitBreakerConfig>();
            mockConfig.Setup(m => m.GetMinimumOperations(It.IsAny<GroupKey>())).Returns(1);
            mockConfig.Setup(m => m.GetThresholdPercentage(It.IsAny<GroupKey>())).Returns(25);
            mockConfig.Setup(m => m.GetTrippedDurationMillis(It.IsAny<GroupKey>())).Returns(30000);
            mockConfig.Setup(m => m.GetForceTripped(It.IsAny<GroupKey>())).Returns(false);
            mockConfig.Setup(m => m.GetForceFixed(It.IsAny<GroupKey>())).Returns(true);

            // We'll call IsAllowing() three times. The first two should force the breaker fixed,
            // the third should un-force it. The breaker should have tripped by then, so the third
            // call should show the breaker disallowing the call.
            mockConfig.SetupSequence(m => m.GetForceFixed(It.IsAny<GroupKey>()))
                .Returns(true)
                .Returns(true)
                .Returns(false);

            var breaker = new FailurePercentageCircuitBreaker(AnyKey, manualClock, mockMetrics.Object, mockEvents.Object, mockConfig.Object, new DefaultMjolnirLogFactory());


            // Act / Assert

            Assert.True(breaker.IsAllowing()); // Will have tripped internally.
            Assert.True(breaker.IsAllowing()); // Continues to allow even when tripped.

            // Test that when the the forced fix property is no longer true, IsAllowing() then rejects.
            // The Mock sequencing enables this.
            
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
            var metricEvents = new Mock<IMetricEvents>();
            var breaker = new BreakerBuilder(1, 1, "Test") // Trip at 1 op, 1% failing.
                .WithMockMetrics(mockMetrics)
                .WithWaitMillis(durationMillis)
                .WithMetricEvents(metricEvents.Object)
                .Create();
            breaker.IsAllowing(); // Trip the breaker.
            metricEvents.Verify(m => m.BreakerTripped("Test"));
            metricEvents.ResetCalls();

            breaker.IsAllowing(); // Make another call, which should bail immediately (and not re-trip).

            // Best way to test this right now is to make sure we don't fire a metric event for the state change.
            metricEvents.Verify(m => m.BreakerTripped(It.IsAny<string>()), Times.Never);
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
                var config = CreateMockBreakerConfig(_breakerTotal, _breakerPercent, 30000);
                var breaker = new FailurePercentageCircuitBreaker(GroupKey.Named("Test"), mockMetrics.Object, new IgnoringMetricEvents(), config.Object, new DefaultMjolnirLogFactory());

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

        internal static Mock<IFailurePercentageCircuitBreakerConfig> CreateMockBreakerConfig(long minimumOperations, int thresholdPercentage, long trippedDurationMillis = 30000, bool forceTripped = false, bool forceFixed = false)
        {

            var mock = new Mock<IFailurePercentageCircuitBreakerConfig>();
            mock.Setup(m => m.GetMinimumOperations(It.IsAny<GroupKey>())).Returns(minimumOperations);
            mock.Setup(m => m.GetThresholdPercentage(It.IsAny<GroupKey>())).Returns(thresholdPercentage);
            mock.Setup(m => m.GetTrippedDurationMillis(It.IsAny<GroupKey>())).Returns(trippedDurationMillis);
            mock.Setup(m => m.GetForceTripped(It.IsAny<GroupKey>())).Returns(forceTripped);
            mock.Setup(m => m.GetForceFixed(It.IsAny<GroupKey>())).Returns(forceFixed);
            return mock;
        }
    }

    internal class BreakerBuilder
    {
        private readonly long _minimumOperations;
        private readonly int _failurePercent;
        private readonly string _key;
        
        private long _waitMillis = 30000;
        private IClock _clock = new UtcSystemClock();
        private IMock<ICommandMetrics> _mockMetrics = FailurePercentageCircuitBreakerTests.CreateMockMetricsWithSnapshot(0, 0);
        private IMetricEvents _metricEvents = new Mock<IMetricEvents>().Object;

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
        
        public BreakerBuilder WithMetricEvents(IMetricEvents metricEvents)
        {
            _metricEvents = metricEvents;
            return this;
        }
        
        public FailurePercentageCircuitBreaker Create()
        {
            var config = FailurePercentageCircuitBreakerTests.CreateMockBreakerConfig(_minimumOperations, _failurePercent, _waitMillis);
            return new FailurePercentageCircuitBreaker(GroupKey.Named(_key), _clock, _mockMetrics.Object, _metricEvents, config.Object, new DefaultMjolnirLogFactory());
        }
    }
}
