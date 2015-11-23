using Hudl.Mjolnir.Key;
using System;
using System.Threading;

namespace Hudl.Mjolnir.Bulkhead
{
    internal interface IBulkheadSemaphore
    {
        void Release();
        bool TryEnter();
        int Available { get; }
        string Name { get; }
    }

    internal class SemaphoreBulkhead : IBulkheadSemaphore
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly GroupKey _key;

        internal SemaphoreBulkhead(GroupKey key, int maxConcurrent)
        {
            if (maxConcurrent < 0)
            {
                throw new ArgumentOutOfRangeException("maxConcurrent", maxConcurrent, "Semaphore bulkhead must have a limit >= 0");
            }

            _key = key;
            _semaphore = new SemaphoreSlim(maxConcurrent);
        }

        public string Name
        {
            get { return _key.Name; }
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
