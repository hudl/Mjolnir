using System;

namespace Hudl.Mjolnir.Command.Attribute
{
    /// <summary>
    /// Used on an interface to proxy all of its method calls through a <see cref="Command"/>.
    /// See Command and its constructors for more information.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public sealed class CommandAttribute : System.Attribute
    {
        private const int DefaultTimeout = 15000;

        private readonly string _group;
        private readonly string _breakerKey;
        private readonly string _poolKey;
        private readonly int _timeout;

        /// <see cref="Command(string, string, TimeSpan)"/>
        public CommandAttribute(string group, string isolationKey, int timeout = DefaultTimeout) : this(group, isolationKey, isolationKey, timeout) {}

        /// <see cref="Command(string, string, string, TimeSpan)"/>
        public CommandAttribute(string group, string breakerKey, string poolKey, int timeout = DefaultTimeout)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                throw new ArgumentException("group");
            }

            if (string.IsNullOrWhiteSpace(breakerKey))
            {
                throw new ArgumentException("breakerKey");
            }

            if (string.IsNullOrWhiteSpace(poolKey))
            {
                throw new ArgumentNullException("poolKey");
            }

            if (timeout < 0)
            {
                throw new ArgumentException("timeout");
            }

            _group = group;
            _breakerKey = breakerKey;
            _poolKey = poolKey;
            _timeout = timeout;
        }

        public string Group
        {
            get { return _group; }
        }

        public string BreakerKey
        {
            get { return _breakerKey; }
        }

        public string PoolKey
        {
            get { return _poolKey; }
        }

        public int Timeout
        {
            get { return _timeout; }
        }
    }
}