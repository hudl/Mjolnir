using Hudl.Mjolnir.Key;
using System;
using System.Threading;

namespace Hudl.Mjolnir.Bulkhead
{
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
        
        public bool TryEnter()
        {
            return _semaphore.Wait(0);
        }

        public int CountAvailable
        {
            get { return _semaphore.CurrentCount; }
        }
    }
}
