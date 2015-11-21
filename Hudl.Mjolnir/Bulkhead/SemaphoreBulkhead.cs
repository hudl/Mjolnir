using System;
using System.Threading;

namespace Hudl.Mjolnir.Bulkhead
{
    internal interface IBulkheadSemaphore
    {
        void Release();
        bool TryEnter();
        int Available { get; }
    }

    internal class SemaphoreBulkhead : IBulkheadSemaphore
    {
        private readonly SemaphoreSlim _semaphore;

        internal SemaphoreBulkhead(int maxConcurrent)
        {
            if (maxConcurrent < 0)
            {
                throw new ArgumentOutOfRangeException("maxConcurrent", maxConcurrent, "Semaphore bulkhead must have a limit >= 0");
            }

            _semaphore = new SemaphoreSlim(maxConcurrent);

            // TODO gauges, stats
        }

        public void Release()
        {
            _semaphore.Release();
        }

        /// <summary>
        /// Tries to immediately enter the semaphore, returning true if entry was allowed.
        /// Does not wait or block.
        /// 
        /// If you use this method, be sure to call .Release() when done with the semaphore.
        /// </summary>
        public bool TryEnter()
        {
            return _semaphore.Wait(0);
        }

        public int Available
        {
            get { return _semaphore.CurrentCount; }
        }
    }
}
