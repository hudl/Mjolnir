using System;

namespace Hudl.Mjolnir.Command.Attribute
{
    /// <summary>
    /// Used to override the timeout provided by <see cref="CommandAttribute"/> 
    /// for specific methods.
    /// 
    /// Should only be used on interface methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CommandTimeoutAttribute : System.Attribute
    {
        private readonly int _timeout;

        public CommandTimeoutAttribute(int timeout)
        {
            if (timeout < 0)
            {
                throw new ArgumentException("timeout");
            }

            _timeout = timeout;
        }

        public int Timeout
        {
            get { return _timeout; }
        }
    }
}