using System;

namespace Hudl.Mjolnir.Config
{
    /// <summary>
    /// Used only for configs which will never change.
    /// </summary>
    internal class NonObservableMjolnirConfiguration: MjolnirConfiguration
    {
        public override IDisposable Subscribe(IObserver<MjolnirConfiguration> observer)
        {
            // No-op
            return null;
        }
    }
}