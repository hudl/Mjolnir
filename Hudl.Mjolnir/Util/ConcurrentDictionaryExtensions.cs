using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Hudl.Mjolnir.Util
{
    internal static class ConcurrentDictionaryExtensions
    {
        // From http://codereview.stackexchange.com/questions/2025
        public static V GetOrAddSafe<K, V>(this ConcurrentDictionary<K, Lazy<V>> dictionary, K key, Func<K, V> valueFactory)
        {
            var lazy = dictionary.GetOrAdd(key, new Lazy<V>(() => valueFactory(key), LazyThreadSafetyMode.PublicationOnly));
            return lazy.Value;
        }
    }
}