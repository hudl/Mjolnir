using System;
using System.Collections.Generic;

namespace Hudl.Mjolnir.Breaker
{
    /// <summary>
    /// Ignored exception types won't count toward breakers tripping or other error counters.
    /// Useful for things like validation, where the system isn't having any problems and the
    /// caller needs to validate before invoking. This list is most applicable when using
    /// [Command] attributes, since extending Command offers the ability to catch these types
    /// specifically within Execute() - though there may still be some benefit in extended
    /// Commands for validation-like situations where throwing is still desired.
    /// </summary>
    public interface IBreakerExceptionHandler
    {
        bool IsExceptionIgnored(Type type);
    }

    /// <summary>
    /// Default implementation for IBreakerExceptionHandler that uses a set of ignored
    /// Exception Types.
    /// </summary>
    public class BreakerExceptionHandler : IBreakerExceptionHandler
    {
        private readonly HashSet<Type> _ignored;
        
        public BreakerExceptionHandler(HashSet<Type> ignored)
        {
            // Defensive copy to avoid caller modifying the set after passing.
            _ignored = (ignored == null ? new HashSet<Type>() : new HashSet<Type>(ignored));
        }

        public bool IsExceptionIgnored(Type type)
        {
            if (type == null)
            {
                return false;
            }

            return _ignored.Contains(type);
        }
    }
}
