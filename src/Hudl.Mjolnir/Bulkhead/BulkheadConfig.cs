using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using System;

namespace Hudl.Mjolnir.Bulkhead
{
    internal interface IBulkheadConfig
    {
        int GetMaxConcurrent(GroupKey key);

        void AddChangeHandler<T>(GroupKey key, Action<T> onConfigChange);

        // Only really exists so we can log it in a validation error message.
        string GetConfigKey(GroupKey key);
    }

    internal class BulkheadConfig : IBulkheadConfig
    {
        private readonly IMjolnirConfig _config;

        public BulkheadConfig(IMjolnirConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _config = config;
        }

        public int GetMaxConcurrent(GroupKey key)
        {
            // TODO verify null is returned by config impl if not set
            return _config.GetConfig<int?>(GetConfigKey(key), null) ?? _config.GetConfig<int>("mjolnir.bulkhead.default.maxConcurrent", 10);
        }

        public void AddChangeHandler<T>(GroupKey key, Action<T> onConfigChange)
        {
            _config.AddChangeHandler(GetConfigKey(key), onConfigChange);
        }

        public string GetConfigKey(GroupKey key)
        {
            return $"mjolnir.bulkhead.{key}.maxConcurrent";
        }
    }
}
