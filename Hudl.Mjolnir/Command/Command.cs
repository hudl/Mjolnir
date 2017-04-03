using System;
using System.Collections.Concurrent;
using Hudl.Mjolnir.Key;

namespace Hudl.Mjolnir.Command
{
    /// <summary>
    /// Abstract class for <see cref="Command">Command</see>. Used mainly as a
    /// holder for a few shared/static properties.
    /// </summary>
    public abstract class Command
    {
        // TODO move this comment somewhere (or delete it)

        /// <summary>
        /// If this is set to true then all calls wrapped in a Mjolnir command will ignore the default timeout.
        /// This is likely to be useful when debugging Command decorated methods, however it is not advisable to use in a production environment since it disables 
        /// some of Mjolnir's key features. 
        /// </summary>
        
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
    }
}
