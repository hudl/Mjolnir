namespace Hudl.Mjolnir.Metrics
{
    internal class MetricsSnapshot
    {
        internal long Total { get; private set; }
        internal int ErrorPercentage { get; private set; }

        internal MetricsSnapshot(long total, int errorPercentage)
        {
            Total = total;
            ErrorPercentage = errorPercentage;
        }
    }
}