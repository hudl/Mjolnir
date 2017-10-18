using Hudl.Mjolnir.External;
using System;
using System.Collections.Generic;

namespace Hudl.Mjolnir.Breaker
{
    /// <summary>
    /// Default implementation for IBreakerExceptionHandler that uses a set of ignored
    /// Exception Types.
    /// </summary>
    public class IgnoredExceptionHandler : IBreakerExceptionHandler
    {
        private readonly HashSet<Type> _ignored;

        public IgnoredExceptionHandler(HashSet<Type> ignored)
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
