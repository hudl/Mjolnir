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
        private readonly bool _ignoreTimeout;

        // See Mjolnir's Command constructors.
        public CommandAttribute(string group, string isolationKey, int timeout = DefaultTimeout) : this(group, isolationKey, isolationKey, timeout) { }

        public CommandAttribute(string group, string isolationKey, bool ignoreTimeout)
            : this(group, isolationKey, isolationKey)
        {
            _ignoreTimeout = ignoreTimeout;
        }

        public CommandAttribute(string group, string breakerKey, string poolKey, bool ignoreTimeout)
            : this(group, breakerKey, poolKey)
        {
            _ignoreTimeout = ignoreTimeout;
        }

        // See Mjolnir's Command constructors.
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

        public bool IgnoreTimeout
        {
            get { return _ignoreTimeout;}
        }
    }
}
