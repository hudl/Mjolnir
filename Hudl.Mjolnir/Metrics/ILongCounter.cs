namespace Hudl.Mjolnir.Metrics
{
    internal interface ILongCounter
    {
        void Increment();
        long Get();
    }
}