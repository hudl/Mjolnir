using System;

namespace Hudl.Mjolnir.Config
{
    public abstract class BulkheadConfiguration: IObservable<BulkheadConfiguration>
    {
        protected int _maxConcurrent;

        /// <summary>
        /// Bulkhead Maximum - The number of Commands that can execute in the Bulkhead concurrently before subsequent 
        /// Command attempts are rejected.
        /// </summary>
        public virtual int MaxConcurrent
        {
            get => _maxConcurrent;
            set => _maxConcurrent = value;
        }

        protected BulkheadConfiguration()
        {
            // Set default value
            _maxConcurrent = 10;
        }

        public abstract IDisposable Subscribe(IObserver<BulkheadConfiguration> observer);
    }
}