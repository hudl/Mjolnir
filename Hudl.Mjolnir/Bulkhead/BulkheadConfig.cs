using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using System;

namespace Hudl.Mjolnir.Bulkhead
{
    internal class BulkheadConfig : IBulkheadConfig
    {
        private const int DefaultMaxConcurrent = 10;

        private readonly IMjolnirConfig _config;
        
        public BulkheadConfig(IMjolnirConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public int GetMaxConcurrent(GroupKey key)
        {
            // TODO verify null is returned by config impl if not set
            return _config.GetConfig<int?>(GetConfigKey(key), null) ?? _config.GetConfig("mjolnir.bulkhead.default.maxConcurrent", DefaultMaxConcurrent);
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
