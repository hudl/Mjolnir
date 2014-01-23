using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Hudl.Mjolnir.Tests.Helper;
using Hudl.Mjolnir.Util;
using Xunit;

namespace Hudl.Mjolnir.Tests.Util
{
    class ConcurrentDictionaryExtensionsTests : TestFixture
    {
        [Fact]
        public async Task GetOrAddSafe_OnConcurrentAccess_ReturnsSameInstance()
        {
            const string key = "key";
            var dictionary = new ConcurrentDictionary<string, Lazy<object>>();

            var valueFactory = new Func<string, object>(k =>
            {
                Thread.Sleep(100);
                return new { };
            });

            var r1 = await Task.Run(() => dictionary.GetOrAddSafe(key, valueFactory));
            var r2 = await Task.Run(() => dictionary.GetOrAddSafe(key, valueFactory));
            Thread.Sleep(10);
            var r3 = await Task.Run(() => dictionary.GetOrAddSafe(key, valueFactory));

            Assert.True(r1 == r2 && r2 == r3);
        }
    }
}
