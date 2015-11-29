using Hudl.Config;
using Hudl.Mjolnir.Key;
using System;

// TODO remove IStats from new BaseCommand and new invokers

namespace Hudl.Mjolnir.Command
{
    /// <summary>
    /// An operation that needs isolation during failure. When passed to an Invoke method on the
    /// <see cref="CommandInvoker"/>, the operation implemented in the Execute method receives
    /// protection via timeouts, circuit breakers, and bulkheads.
    /// 
    /// For a detailed overview, see https://github.com/hudl/Mjolnir/wiki.
    /// </summary>
    public abstract class BaseCommand : Command
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(2000);

        private readonly GroupKey _group;
        private readonly string _name;
        private readonly GroupKey _breakerKey;
        private readonly GroupKey _bulkheadKey;
        private readonly TimeSpan _constructorTimeout;
        
        // 0 == not yet invoked, > 0 == invoked
        // This is modified by the invoker with consideration for concurrency.
        internal int HasInvoked = 0;

        // Stores the time spent in Execute() or ExecuteAsync().
        // Set by the component that actually calls the Execute* method.
        internal double ExecutionTimeMillis { get; set; }
        
        /// <summary>
        /// Constructs the command.
        /// 
        /// The group is used as part of the command's <see cref="Name">Name</see>.
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
        /// <param name="defaultTimeout">Timeout to enforce if not otherwise provided. </param>
        protected BaseCommand(string group, string breakerKey, string bulkheadKey, TimeSpan? defaultTimeout)
            : this(group, null, breakerKey, bulkheadKey, defaultTimeout = null)
        { }

        internal BaseCommand(string group, string name, string breakerKey, string bulkheadKey, TimeSpan? defaultTimeout = null)
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

            if (defaultTimeout != null && defaultTimeout.Value.TotalMilliseconds <= 0)
            {
                throw new ArgumentException("Positive default timeout is required", "defaultTimeout");
            }

            _group = GroupKey.Named(group);
            _name = string.IsNullOrWhiteSpace(name) ? GenerateAndCacheName(Group) : CacheProvidedName(Group, name);
            _breakerKey = GroupKey.Named(breakerKey);
            _bulkheadKey = GroupKey.Named(bulkheadKey);
            _constructorTimeout = defaultTimeout ?? DefaultTimeout;
        }

        // Constructor Timeout: Value defined in the Command constructor.
        // Configured Timeout: Value provided by config.
        // Invocation Timeout: Value passed into the Invoke() / InvokeAsync() call.
        internal TimeSpan DetermineTimeout(long? invocationTimeoutMillis = null)
        {
            // Prefer the invocation timeout first. It's more specific than the Constructor
            // Timeout (which is defined by the command author and is treated as a "catch-all"),
            // It's also more specific than the Configured Timeout, which is a way to tune
            // the Constructor Timeout more specifically (i.e. still "catch-all" behavior).
            if (invocationTimeoutMillis.HasValue && invocationTimeoutMillis.Value >= 0)
            {
                return TimeSpan.FromMilliseconds(invocationTimeoutMillis.Value);
            }
            
            var configured = GetTimeoutConfigurableValue(_name).Value;

            // We don't want to include 0 here. Since this comes from a potentially non-nullable
            // ConfigurableValue, it's possible (and probably likely) that an unconfigured
            // timeout will return a default(long), which will be 0.
            if (configured > 0)
            {
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
            return TimeoutConfigCache.GetOrAdd(commandName, n => new ConfigurableValue<long>("mjolnir.command." + commandName + ".Timeout"));
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
