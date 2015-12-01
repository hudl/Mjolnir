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

        public TResult Value { get { return _value; } }
        public Exception Exception { get { return _exception; } }
        public bool WasSuccess { get { return _exception == null; } }

        internal CommandResult(TResult value, Exception exception = null)
        {
            _value = value;
            _exception = exception;
        }
    }
}