﻿using Hudl.Mjolnir.Isolation;

namespace Hudl.Mjolnir.Tests.Helper
{
    internal class AlwaysSuccessfulIsolationSemaphore : IIsolationSemaphore
    {
        public bool TryEnter()
        {
            return true;
        }

        public void Release()
        {
            // No-op.
        }
    }
}
