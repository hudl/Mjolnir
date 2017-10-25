using System;

namespace Hudl.Mjolnir.External
{
    /// <summary>
    /// Ignored exception types won't count toward breakers tripping or other error counters.
    /// Useful for things like validation, where the system isn't having any problems and the
    /// caller needs to validate before invoking.
    /// 
    /// Note that ignored exceptions will still be thrown back through Execute/ExecuteAsync.
    /// They simply won't count toward circuit breaker errors.
    /// </summary>
    public interface IBreakerExceptionHandler
    {
        /// <summary>
        /// Returns true if the exception should be ignored by circuit breakers when counting
        /// errors. Useful for excluding things like ArgumentExceptions, where the error is likely
        /// not a downstream system error and instead more likely an error/bug on the calling side.
        /// </summary>
        bool IsExceptionIgnored(Type type);
    }
}
