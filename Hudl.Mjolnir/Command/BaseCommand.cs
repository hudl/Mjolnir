using Hudl.Config;
using Hudl.Mjolnir.Key;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hudl.Mjolnir.Command
{
    public abstract class BaseCommand : Command
    {
        internal readonly TimeSpan? Timeout;
        
        internal readonly bool TimeoutsIgnored;

        private readonly GroupKey _group;
        private readonly string _name;
        private readonly GroupKey _breakerKey;
        private readonly GroupKey _poolKey;
        
        // 0 == not yet invoked, > 0 == invoked
        internal int _hasInvoked = 0;

        /// <summary>
        /// Constructs the Command.
        /// 
        /// The group is used as part of the Command's <see cref="Name">Name</see>.
        /// If the group contains dots, they'll be converted to dashes.
        /// 
        /// The provided <code>isolationKey</code> will be used as both the
        /// breaker and pool keys.
        /// 
        /// Command timeouts can be configured at runtime. Configuration keys
        /// follow the form: <code>mjolnir.group-key.CommandClassName.Timeout</code>
        /// (i.e. <code>mjolnir.[Command.Name].Timeout</code>). If not
        /// configured, the provided <code>defaultTimeout</code> will be used.
        /// 
        /// </summary>
        /// <param name="group">Logical grouping for the command, usually the owning team. Avoid using dots.</param>
        /// <param name="isolationKey">Breaker and pool key to use.</param>
        /// <param name="defaultTimeout">Timeout to enforce if not otherwise configured.</param>
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
        /// <param name="poolKey">Pool to use for this command.</param>
        /// <param name="defaultTimeout">Timeout to enforce if not otherwise configured.</param>
        protected BaseCommand(string group, string breakerKey, string poolKey, TimeSpan defaultTimeout)
            : this(group, null, breakerKey, poolKey, defaultTimeout)
        { }

        internal BaseCommand(string group, string name, string breakerKey, string poolKey, TimeSpan defaultTimeout)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                throw new ArgumentNullException("group");
            }

            if (string.IsNullOrWhiteSpace(breakerKey))
            {
                throw new ArgumentNullException("breakerKey");
            }

            if (string.IsNullOrWhiteSpace(poolKey))
            {
                throw new ArgumentNullException("poolKey");
            }

            if (defaultTimeout.TotalMilliseconds <= 0)
            {
                throw new ArgumentException("Positive default timeout is required", "defaultTimeout");
            }

            _group = GroupKey.Named(group);
            _name = string.IsNullOrWhiteSpace(name) ? GenerateAndCacheName(Group) : CacheProvidedName(Group, name);
            _breakerKey = GroupKey.Named(breakerKey);
            _poolKey = GroupKey.Named(poolKey);
            
            Timeout = GetCommandTimeout(defaultTimeout);
        }

        private TimeSpan? GetCommandTimeout(TimeSpan defaultTimeout)
        {
            if (IgnoreCommandTimeouts.Value)
            {
                // TODO make sure the new null timeout here is handled everywhere appropriately

                // TODO log?
                return null;
            }

            var timeout = GetTimeoutConfigurableValue(_name).Value;
            if (timeout <= 0)
            {
                timeout = (long) defaultTimeout.TotalMilliseconds;
            }
            else
            {
                // TODO log?
                //_log.DebugFormat("Timeout configuration override for this command of {0}", timeout);
            }

            return TimeSpan.FromMilliseconds(timeout);
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

        internal GroupKey PoolKey
        {
            get { return _poolKey; }
        }

        internal string StatsPrefix
        {
            get { return "mjolnir command " + Name; }
        }
    }
    
    public sealed class Void
    {
        
    }

    public abstract class AsyncCommand<TResult> : BaseCommand
    {
        public AsyncCommand(string group, string isolationKey, TimeSpan defaultTimeout) : base(group, isolationKey, defaultTimeout)
        { }

        protected internal abstract Task<TResult> ExecuteAsync(CancellationToken cancellationToken);
    }
    
    public abstract class SyncCommand<TResult> : BaseCommand
    {
        public SyncCommand(string group, string isolationKey, TimeSpan defaultTimeout) : base(group, isolationKey, defaultTimeout)
        { }

        protected internal abstract TResult Execute(CancellationToken cancellationToken);
    }

    public abstract class SyncCommand : BaseCommand
    {
        public SyncCommand(string group, string isolationKey, TimeSpan defaultTimeout) : base(group, isolationKey, defaultTimeout)
        { }

        protected internal abstract void Execute(CancellationToken cancellationToken);
    }
}
