using System.Threading;

namespace Hudl.Mjolnir.Metrics
{
    internal class InterlockingLongCounter : ILongCounter
    {
        private long _count;

        public void Increment()
        {
            Interlocked.Increment(ref _count);
        }

        public long Get()
        {
            return Interlocked.Read(ref _count);
        }
    }
}