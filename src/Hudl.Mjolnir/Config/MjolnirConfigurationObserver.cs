using System;

namespace Hudl.Mjolnir.Config
{
    internal sealed class MjolnirConfigurationObserver<T> : IObserver<MjolnirConfiguration>
    {
        private T _currentValue;
        private readonly Func<MjolnirConfiguration, T> _expression;
        private readonly Action<T> _onChange;
        internal MjolnirConfigurationObserver(MjolnirConfiguration currentConfig, 
            Func<MjolnirConfiguration, T> propertyToCheck, Action<T> onChange)
        {
            _expression = propertyToCheck;
            _currentValue = _expression(currentConfig);
            _onChange = onChange;
        }

        public void OnCompleted()
        {
            // No-op
        }

        public void OnError(Exception error)
        {
            // No-op
        }

        public void OnNext(MjolnirConfiguration value)
        {
            var newValue = _expression(value);
            var hasChanged = !Equals(_currentValue, newValue);

            if (!hasChanged) return;
            
            try
            {
                // Invoke onChange action to act apon config change.
                _onChange(newValue);
            }
            catch (Exception)
            {
                // Even if onChange implementation returns exception we still want to process other changes. Exception 
                // here should not affect observable implementation. 
            }
            finally
            {
                _currentValue = newValue;
            }
        }
    }
}