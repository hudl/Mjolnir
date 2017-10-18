using Hudl.Mjolnir.External;
using System;

namespace Hudl.Mjolnir.Log
{
    internal class DefaultMjolnirLog<T> : IMjolnirLog<T>
    {
        public void Debug(string message)
        {
            return;
        }

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

        public void SetLogName(string name)
        {
            return;
        }
    }
}
