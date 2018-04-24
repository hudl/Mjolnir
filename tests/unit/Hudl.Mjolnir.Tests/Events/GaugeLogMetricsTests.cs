using System;
using Hudl.Mjolnir.Events;
using Hudl.Mjolnir.External;
using Moq;
using Xunit;

namespace Hudl.Mjolnir.Tests.Events
{
    public class GaugeLogMetricsTests
    {
        [Fact]
        public void RejectionsLoggedInGaugeCorrectly()
        {
            var mockLogFactory = new Mock<IMjolnirLogFactory>();
            var mockLogger = new Mock<IMjolnirLog<GaugeLogMetrics>>();
            mockLogFactory.Setup(i => i.CreateLog<GaugeLogMetrics>()).Returns(mockLogger.Object);
            var gaugeLogMetrics = new GaugeLogMetrics(mockLogFactory.Object);
            Func<string, int, bool> logMessageCheck = (s, i) =>
            {
                var containsRejections = s.Contains("BulkheadRejections") && s.Contains($"Rejections={i}");
                return containsRejections;
            };
            gaugeLogMetrics.RejectedByBulkhead("test", "test-command");
            gaugeLogMetrics.RejectedByBulkhead("test", "test-command");
            gaugeLogMetrics.RejectedByBulkhead("test", "test-command");
            gaugeLogMetrics.BulkheadGauge("test", "test", 2, 0);
            gaugeLogMetrics.RejectedByBulkhead("test", "test-command");
            gaugeLogMetrics.BulkheadGauge("test", "test", 2, 0);
            mockLogger.Verify(i => i.Debug(It.Is<string>(s => logMessageCheck(s, 3))), Times.Once);
            mockLogger.Verify(i => i.Debug(It.Is<string>(s => logMessageCheck(s, 1))), Times.Once);
        }
    }
}