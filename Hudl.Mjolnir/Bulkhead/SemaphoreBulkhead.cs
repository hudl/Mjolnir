using Hudl.Config;
using System.Threading;
using System.Threading.Tasks;

namespace Hudl.Mjolnir.Bulkhead
{
    internal interface IBulkheadSemaphore
    {
        void Release();
        bool TryEnter();
        //void Wait(CancellationToken ct);
        //Task WaitAsync(CancellationToken ct);
    }

    internal class SemaphoreBulkhead : IBulkheadSemaphore
    {
        private readonly SemaphoreSlim _semaphore;

        internal SemaphoreBulkhead(int maxConcurrent)
        {
            // TODO validate value?
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
        
        /// <summary>
        /// Blocks until the semaphore is available or cancellation occurs (via the provided
        /// CancellationToken).
        /// </summary>
        //public void Wait(CancellationToken ct)
        //{
        //    _semaphore.Wait(ct);
        //}

        /// <summary>
        /// Asynchronously waits until the semaphore is available or cancellation occurs (via the 
        /// provided CancellationToken).
        /// </summary>
        //public async Task WaitAsync(CancellationToken ct)
        //{
        //    await _semaphore.WaitAsync(ct);
        //}
    }
}
