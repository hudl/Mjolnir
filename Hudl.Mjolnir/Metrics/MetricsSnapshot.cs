namespace Hudl.Mjolnir.Metrics
{
    internal class MetricsSnapshot
    {
        public long Total { get; private set; }
        public int ErrorPercentage { get; private set; }

        public MetricsSnapshot(long total, int errorPercentage)
        {
            Total = total;
            ErrorPercentage = errorPercentage;
        }
    }
}