using System;

namespace Hudl.Mjolnir.Config
{
    /// <summary>
    /// Used only for configs which will never change.
    /// </summary>
    internal class NonObservableBulkheadConfiguration: BulkheadConfiguration
    {
        public override IDisposable Subscribe(IObserver<BulkheadConfiguration> observer)
        {
            // No-op
            return null;
        }
    }
}