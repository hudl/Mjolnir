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
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(MjolnirConfiguration value)
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