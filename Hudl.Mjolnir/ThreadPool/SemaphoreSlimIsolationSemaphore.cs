using System.Threading;
using Hudl.Config;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Util;

namespace Hudl.Mjolnir.ThreadPool
{
    internal class SemaphoreSlimIsolationSemaphore : IIsolationSemaphore
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly GroupKey _key;
        private readonly int _maxConcurrent;

        // ReSharper disable NotAccessedField.Local
        // Don't let these get garbage collected.
        private readonly GaugeTimer _timer;
        // ReSharper restore NotAccessedField.Local

        internal SemaphoreSlimIsolationSemaphore(GroupKey key, IConfigurableValue<int> maxConcurrent, IConfigurableValue<long> gaugeIntervalMillisOverride = null)
        {
            _key = key;
            
            // Note: Changing the semaphore maximum at runtime is not currently supported.
            _maxConcurrent = maxConcurrent.Value;
            _semaphore = new SemaphoreSlim(_maxConcurrent);
        }
        
        public bool TryEnter()
        {
            return _semaphore.Wait(0);
        }

        public void Release()
        {
            _semaphore.Release();
        }
    }
}