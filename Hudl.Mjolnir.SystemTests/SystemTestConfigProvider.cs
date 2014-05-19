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
            //{ "stats.riemann.isEnabled", false },
            //{ "command.system-test.HttpClient.Timeout", 15000 },
            { "mjolnir.gaugeIntervalMillis", 500 },
            
            //{ "mjolnir.pools.system-test.threadCount", 10 },
            //{ "mjolnir.pools.system-test.queueLength", 10 },
            /*{ "", false },
            { "", false },
            { "", false },
            { "", false },
            { "", false },
            { "", false },
            { "", false },*/
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
            throw new NotImplementedException();
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