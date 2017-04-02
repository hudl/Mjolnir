using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Hudl.Config;
using Hudl.Mjolnir.Key;

namespace Hudl.Mjolnir.Command
{
    /// <see cref="Command"/>
    /// <typeparam name="TResult">The type of the result returned by the Command's execution.</typeparam>
    public interface ICommand<TResult>
    {
        /// <summary>
        /// Invoke the Command synchronously. See <see cref="Command#Invoke()"/>.
        /// </summary>
        TResult Invoke();

        /// <summary>
        /// Invoke the Command asynchronously. See <see cref="Command#InvokeAsync()"/>.
        /// </summary>
        Task<TResult> InvokeAsync();
    }

    /// <summary>
    /// Abstract class for <see cref="Command">Command</see>. Used mainly as a
    /// holder for a few shared/static properties.
    /// </summary>
    public abstract class Command
    {
        protected static readonly ConfigurableValue<bool> UseCircuitBreakers = new ConfigurableValue<bool>("mjolnir.useCircuitBreakers", true);

        /// <summary>
        /// If this is set to true then all calls wrapped in a Mjolnir command will ignore the default timeout.
        /// This is likely to be useful when debugging Command decorated methods, however it is not advisable to use in a production environment since it disables 
        /// some of Mjolnir's key features. 
        /// </summary>
        protected static readonly ConfigurableValue<bool> IgnoreCommandTimeouts = new ConfigurableValue<bool>("mjolnir.ignoreTimeouts", false);

        /// <summary>
        /// Cache of known command names, keyed by Type and group key. Helps
        /// avoid repeatedly generating the same Name for every distinct command
        /// instance.
        /// </summary>
        protected static readonly ConcurrentDictionary<Tuple<Type, GroupKey>, string> GeneratedNameCache = new ConcurrentDictionary<Tuple<Type, GroupKey>, string>();

        /// <summary>
        /// Cache of known command names, keyed by provided name and group key. Helps
        /// avoid repeatedly generating the same Name for every distinct command.
        /// </summary>
        protected static readonly ConcurrentDictionary<Tuple<string, GroupKey>, string> ProvidedNameCache = new ConcurrentDictionary<Tuple<string, GroupKey>, string>();

        /// <summary>
        /// Maps command names to IConfigurableValues with command timeouts.
        /// 
        /// This is only internal so that we can look at it during unit tests.
        /// </summary>
        internal static readonly ConcurrentDictionary<string, IConfigurableValue<long>> TimeoutConfigCache = new ConcurrentDictionary<string, IConfigurableValue<long>>();
    }
}
