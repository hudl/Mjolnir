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
