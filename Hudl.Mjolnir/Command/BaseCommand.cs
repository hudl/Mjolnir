using Hudl.Config;
using Hudl.Mjolnir.Key;
using System;

namespace Hudl.Mjolnir.Command
{
    public abstract class BaseCommand : Command
    {
        private readonly GroupKey _group;
        private readonly string _name;
        private readonly GroupKey _breakerKey;
        private readonly GroupKey _bulkheadKey;
        private readonly TimeSpan _constructorTimeout;
        
        // 0 == not yet invoked, > 0 == invoked
        // This is modified by the invoker with concurrency protections.
        internal int _hasInvoked = 0;

        /// <summary>
        /// Constructs the Command.
        /// 
        /// The group is used as part of the Command's <see cref="Name">Name</see>.
        /// If the group contains dots, they'll be converted to dashes.
        /// 
        /// The provided <code>isolationKey</code> will be used as both the
        /// breaker and bulkhead keys.
        /// 
        /// Command timeouts can be configured at runtime. Configuration keys
        /// follow the form: <code>mjolnir.group-key.CommandClassName.Timeout</code>
        /// (i.e. <code>mjolnir.[Command.Name].Timeout</code>). If not
        /// configured, the provided <code>defaultTimeout</code> will be used.
        /// 
        /// </summary>
        /// <param name="group">Logical grouping for the command, usually the owning team. Avoid using dots.</param>
        /// <param name="isolationKey">Breaker and bulkhead key to use.</param>
        /// <param name="defaultTimeout">Timeout to enforce if not otherwise provided.</param>
        protected BaseCommand(string group, string isolationKey, TimeSpan defaultTimeout)
            : this(group, null, isolationKey, isolationKey, defaultTimeout)
        { }

        /// <summary>
        /// Constructs the Command.
        /// 
        /// The group is used as part of the Command's <see cref="Name">Name</see>.
        /// If the group contains dots, they'll be converted to dashes.
        /// 
        /// Command timeouts can be configured at runtime. Configuration keys
        /// follow the form: <code>mjolnir.group-key.CommandClassName.Timeout</code>
        /// (i.e. <code>mjolnir.[Command.Name].Timeout</code>). If not
        /// configured, the provided <code>defaultTimeout</code> will be used.
        /// 
        /// </summary>
        /// <param name="group">Logical grouping for the command, usually the owning team. Avoid using dots.</param>
        /// <param name="breakerKey">Breaker to use for this command.</param>
        /// <param name="bulkheadKey">Bulkhead to use for this command.</param>
        /// <param name="defaultTimeout">Timeout to enforce if not otherwise provided.</param>
        protected BaseCommand(string group, string breakerKey, string bulkheadKey, TimeSpan defaultTimeout)
            : this(group, null, breakerKey, bulkheadKey, defaultTimeout)
        { }

        internal BaseCommand(string group, string name, string breakerKey, string bulkheadKey, TimeSpan defaultTimeout)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                throw new ArgumentNullException("group");
            }

            if (string.IsNullOrWhiteSpace(breakerKey))
            {
                throw new ArgumentNullException("breakerKey");
            }

            if (string.IsNullOrWhiteSpace(bulkheadKey))
            {
                throw new ArgumentNullException("bulkheadKey");
            }

            if (defaultTimeout.TotalMilliseconds <= 0)
            {
                throw new ArgumentException("Positive default timeout is required", "defaultTimeout");
            }

            _group = GroupKey.Named(group);
            _name = string.IsNullOrWhiteSpace(name) ? GenerateAndCacheName(Group) : CacheProvidedName(Group, name);
            _breakerKey = GroupKey.Named(breakerKey);
            _bulkheadKey = GroupKey.Named(bulkheadKey);
            _constructorTimeout = defaultTimeout;
        }

        // Constructor Timeout: Value defined in the Command constructor.
        // Configured Timeout: Value provided by config.
        // Invocation Timeout: Value passed into the Invoke() / InvokeAsync() call.
        internal TimeSpan DetermineTimeout(long? invocationTimeoutMillis)
        {
            // Prefer the invocation timeout first. It's more specific than the Constructor
            // Timeout (which is defined by the command author and is treated as a "catch-all"),
            // It's also more specific than the Configured Timeout, which is a way to tune
            // the Constructor Timeout more specifically (i.e. still "catch-all" behavior).
            if (invocationTimeoutMillis.HasValue && invocationTimeoutMillis.Value >= 0)
            {
                // TODO anything if the passed-in value is < 0? log a warn? probably don't
                // want to kill the call.
                return TimeSpan.FromMilliseconds(invocationTimeoutMillis.Value);
            }

            // TODO allow configured "0" values here to immediately time out calls?

            var configured = GetTimeoutConfigurableValue(_name).Value;
            if (configured > 0)
            {
                // TODO anything if the configured value is <= 0? log? probably don't kill the call.
                return TimeSpan.FromMilliseconds(configured);
            }

            return _constructorTimeout;
        }

        private string CacheProvidedName(GroupKey group, string name)
        {
            var cacheKey = new Tuple<string, GroupKey>(name, group);
            return ProvidedNameCache.GetOrAdd(cacheKey, t => cacheKey.Item2.Name.Replace(".", "-") + "." + name.Replace(".", "-"));
        }

        // Since creating the Command's name is non-trivial, we'll keep a local
        // cache of them.
        private string GenerateAndCacheName(GroupKey group)
        {
            var type = GetType();
            var cacheKey = new Tuple<Type, GroupKey>(type, group);
            return GeneratedNameCache.GetOrAdd(cacheKey, t =>
            {
                var className = cacheKey.Item1.Name;
                if (className.EndsWith("Command", StringComparison.InvariantCulture))
                {
                    className = className.Substring(0, className.LastIndexOf("Command", StringComparison.InvariantCulture));
                }

                return cacheKey.Item2.Name.Replace(".", "-") + "." + className;
            });
        }

        private static IConfigurableValue<long> GetTimeoutConfigurableValue(string commandName)
        {
            return TimeoutConfigCache.GetOrAdd(commandName, n => new ConfigurableValue<long>("command." + commandName + ".Timeout"));
        }

        internal string Name
        {
            get { return _name; }
        }

        internal GroupKey Group
        {
            get { return _group; }
        }

        internal GroupKey BreakerKey
        {
            get { return _breakerKey; }
        }

        internal GroupKey BulkheadKey
        {
            get { return _bulkheadKey; }
        }

        internal string StatsPrefix
        {
            get { return "mjolnir command " + Name; }
        }
    }
}
