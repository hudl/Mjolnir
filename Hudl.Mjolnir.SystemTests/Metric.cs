namespace Hudl.Mjolnir.SystemTests
{
    internal class Metric
    {
        public double OffsetSeconds { get; private set; }
        public string Service { get; private set; }
        public string Status { get; private set; }
        public float? Value { get; private set; }

        public Metric(double offsetSeconds, string service, string status, float? value)
        {
            OffsetSeconds = offsetSeconds;
            Service = service;
            Status = status;
            Value = value;
        }

        public string ToCsvLine()
        {
            return string.Format("{0},{1},{2},{3}", OffsetSeconds, Service, Status, Value);
        }
    }
}