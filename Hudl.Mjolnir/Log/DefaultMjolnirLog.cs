using Hudl.Mjolnir.External;
using System;

namespace Hudl.Mjolnir.Log
{
    // TODO is ignoring the right default? maybe a console log instead?

    internal class DefaultMjolnirLog : IMjolnirLog
    {
        public void Error(string message)
        {
            return;
        }

        public void Error(string message, Exception exception)
        {
            return;
        }

        public void Info(string message)
        {
            return;
        }
    }
}
