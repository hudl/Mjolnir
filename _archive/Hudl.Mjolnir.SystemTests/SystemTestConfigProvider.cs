using System;
using System.Collections.Generic;
using System.Linq;
using Hudl.Config;

namespace Hudl.Mjolnir.SystemTests
{
    internal class SystemTestConfigProvider : IConfigurationProvider
    {
        private static readonly Dictionary<string, object> Values = new Dictionary<string, object>
        {
            { "mjolnir.useCircuitBreakers", true },
            { "mjolnir.gaugeIntervalMillis", 500 },
        }; 

        public T Get<T>(string configKey)
        {
            return ConvertValue<T>(Values.ContainsKey(configKey) ? Values[configKey] : null);
        }

        public object Get(string configKey)
        {
            return Values.ContainsKey(configKey) ? Values[configKey] : null;
        }

        public void Set(string configKey, object value)
        {
            Values[configKey] = value;
        }

        public void Delete(string configKey)
        {
            throw new NotImplementedException();
        }

        public string[] GetKeys(string prefix)
        {
            return Values.Keys.ToArray();
        }

        public T ConvertValue<T>(object value)
        {
            return DefaultValueConverter.ConvertValue<T>(value);
        }

        public event ConfigurationChangedHandler ConfigurationChanged;
    }
}