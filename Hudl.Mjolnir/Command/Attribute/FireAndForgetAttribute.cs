using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hudl.Mjolnir.Command.Attribute
{
    /// <summary>
    /// Used on methods of <see cref="CommandAttribute"/> interfaces
    /// to indicate that the call should return immediately and execute
    /// the method on a background thread.
    /// 
    /// Should only be used on interface methods.
    /// </summary>
    [Obsolete("Use [FireAndForget] from the Hudl.Mjolnir.Attributes package instead")]
    [AttributeUsage(AttributeTargets.Method)]
    public class FireAndForgetAttribute : System.Attribute
    {

    }
}
