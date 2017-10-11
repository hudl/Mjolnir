using System;

namespace Hudl.Mjolnir.Command
{
    /// <summary>
    /// Wraps the result of a command's execution. If WasSuccess is true, the Value will have the
    /// result of the execution. If WasSuccess is false, the Value will not be correct, and the
    /// Exception field will have the causing exception
    /// </summary>
    /// <typeparam name="TResult">Type of the command's execution result.</typeparam>
    public sealed class CommandResult<TResult>
    {
        private readonly TResult _value;
        private readonly Exception _exception;

        /// <summary>
        /// Whether or not command execution was successful. If the command failed or Mjolnir
        /// rejected the command, this will be false.
        /// </summary>
        public bool WasSuccess { get { return _exception == null; } }

        /// <summary>
        /// Gets the Value of the command execution. This will throw an exception if accessed
        /// when WasSuccess == false, so check the WasSuccess property before retrieving.
        /// </summary>
        public TResult Value
        {
            get
            {
                if (!WasSuccess)
                {
                    throw new InvalidOperationException("Cannot access CommandResult Value property for unsuccessful result. Check WasSuccess before accessing the Value.");
                }
                return _value;
            }
        }

        /// <summary>
        /// If WasSuccess == false, will contain the causing exception.
        /// </summary>
        public Exception Exception { get { return _exception; } }
        
        internal CommandResult(TResult value, Exception exception = null)
        {
            _value = value;
            _exception = exception;
        }
    }
}