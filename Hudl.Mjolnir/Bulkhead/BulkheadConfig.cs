using Hudl.Mjolnir.Key;
using System;
using Hudl.Mjolnir.Config;

namespace Hudl.Mjolnir.Bulkhead
{
    internal class BulkheadConfig : IBulkheadConfig
    {
        private readonly MjolnirConfiguration _config;
        
        public BulkheadConfig(MjolnirConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public int GetMaxConcurrent(GroupKey key)
        {
            return _config.GetBulkheadConfiguration(key.Name).MaxConcurrent;
        }
    }
}
