using Hudl.Config;
using Hudl.Mjolnir.Key;
using System;

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
        /// follow the form: <code>mjolnir.command.[Command.Name].Timeout</code>). If not
        /// configured, the provided <code>defaultTimeout</code> will be used. If no
        /// <code>defaultTimeout</code> is provided, a Mjolnir-wide timeout of 2 seconds
        /// will be applied.
        /// 
        /// </summary>
        /// <param name="group">Logical grouping for the command, usually the owning team. Avoid using dots.</param>
        /// <param name="breakerKey">Breaker to use for this command.</param>
        /// <param name="bulkheadKey">Bulkhead to use for this command.</param>
        /// <param name="defaultTimeout">Timeout to enforce if not otherwise provided. </param>
        protected BaseCommand(string group, string breakerKey, string bulkheadKey, TimeSpan? defaultTimeout)
            : this(group, null, breakerKey, bulkheadKey, defaultTimeout)
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
                throw new ArgumentException(
                    string.Format("Positive default timeout is required if passed (received invalid timeout of {0}ms)", defaultTimeout.Value.TotalMilliseconds),
                    "defaultTimeout");
            }

            _group = GroupKey.Named(group);
            _name = string.IsNullOrWhiteSpace(name) ? GenerateAndCacheName(Group) : CacheProvidedName(Group, name);
            _breakerKey = GroupKey.Named(breakerKey);
            _bulkheadKey = GroupKey.Named(bulkheadKey);
            _constructorTimeout = defaultTimeout ?? DefaultTimeout;
        }

        // Default Timeout: The system default timeout (2 seconds). Used if nothing else is set.
        // Constructor Timeout: Value defined in the Command constructor.
        // Configured Timeout: Value provided by config.
        // Invocation Timeout: Value passed into the Invoke() / InvokeAsync() call.
        internal TimeSpan DetermineTimeout(long? invocationTimeoutMillis = null)
        {
            // Thoughts on invocation timeout vs. configured timeout:
            //
            // Generally, I'd recommend using one or the other, but not both. Configurable timeouts
            // are useful for changing the value at runtime, but are (at least in our environment)
            // shared across many services, so they don't permit fine-grained control over
            // individual calls. Invocation timeouts allow that fine-grained control, but require a
            // code deploy to change them.
            //
            // Basically, consider the balance between runtime control and fine-grained per-call
            // control when setting timeouts.


            // Prefer the invocation timeout first. It's more specific than the Constructor
            // Timeout (which is defined by the command author and is treated as a "catch-all"),
            // It's also more local than the Configured Timeout, which is a way to tune
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

        private static string CacheProvidedName(GroupKey group, string name)
        {
            var cacheKey = new Tuple<string, GroupKey>(name, group);
            return ProvidedNameCache.GetOrAdd(cacheKey, t => cacheKey.Item2.Name.Replace(".", "-") + "." + name.Replace(".", "-"));
        }

        // Since creating the Command's name is non-trivial, we'll keep a local
        // cache of them. They're accessed frequently (at least once per call), so avoiding all
        // of this string manipulation every time seems like a good idea.
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

        /// <summary>
        /// The generated command name, used for logging and configuration. Follows the form:
        /// <code>group-key.CommandClassName</code>. Some normalization is performed before
        /// generating the name, like removing "." characters from group names and truncating
        /// the class name.
        /// </summary>
        public string Name
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
    }
}
