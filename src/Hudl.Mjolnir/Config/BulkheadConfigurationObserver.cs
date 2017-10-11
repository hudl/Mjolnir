using System;

namespace Hudl.Mjolnir.Config
{
    internal sealed class BulkheadConfigurationObserver<T> : IObserver<BulkheadConfiguration>
    {
        private T _currentValue;
        private readonly Func<BulkheadConfiguration, T> _expression;
        private readonly Action<T> _onChange;
        internal BulkheadConfigurationObserver(BulkheadConfiguration currentConfig, Func<BulkheadConfiguration, T> propertyToCheck, Action<T> onChange)
        {
            _expression = propertyToCheck;
            _currentValue = _expression(currentConfig);
            _onChange = onChange;
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(BulkheadConfiguration value)
        {
            var newValue = _expression(value);
            var hasChanged = !Equals(_currentValue, newValue);

            if (!hasChanged) return;
            
            try
            {
                _onChange(newValue);
            }
            catch (Exception)
            {
                //Purely to make sure that other change events still fire 
            }
            finally
            {
                _currentValue = newValue;
            }
        }
    }
}