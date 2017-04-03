using System;

namespace Hudl.Mjolnir.Attributes
{
    /// <summary>
    /// Used on an interface to proxy all of its method calls through a Mjolnir Command.
    /// See Command and its constructors for more information.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public sealed class CommandAttribute : Attribute
    {
        private const int DefaultTimeout = 15000;

        private readonly string _group;
        private readonly string _breakerKey;
        private readonly string _poolKey;
        private readonly int _timeout;

        // See Mjolnir's Command constructors.
        public CommandAttribute(string group, string isolationKey, int timeout = DefaultTimeout) : this(group, isolationKey, isolationKey, timeout) { }

        // See Mjolnir's Command constructors.
        public CommandAttribute(string group, string breakerKey, string poolKey, int timeout = DefaultTimeout)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                throw new ArgumentNullException(nameof(group));
            }

            if (string.IsNullOrWhiteSpace(breakerKey))
            {
                throw new ArgumentNullException(nameof(breakerKey));
            }

            if (string.IsNullOrWhiteSpace(poolKey))
            {
                throw new ArgumentNullException(nameof(poolKey));
            }

            if (timeout < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
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
