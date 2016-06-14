using System;

namespace Hudl.Mjolnir.Attributes
{
    /// <summary>
    /// Used on methods of <see cref="CommandAttribute"/> interfaces
    /// to indicate that the call should return immediately and execute
    /// the method on a background thread.
    /// 
    /// Should only be used on interface methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    [Obsolete("Will be removed in a future release. Leaving async calls un-awaited is generally not recommended.")]
    public class FireAndForgetAttribute : Attribute
    {

    }
}
