using System;
using System.Linq;
using System.Threading;

namespace Hudl.Mjolnir.Tests.Helper
{
    internal static class Rand
    {
        // From http://stackoverflow.com/a/1344242/29995
        public static string String(int length = 16)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[ThreadSafeRandom.Next(s.Length)])
              .ToArray());
        }
    }

    /// <summary>
    /// Thread-safe random number provider.
    /// 
    /// System.Random is not thread safe, and may return zeroes if accessed
    /// under high contention. Use this instead if you're sharing a number
    /// generator across threads.
    /// 
    /// From http://csharpindepth.com/Articles/Chapter12/Random.aspx
    /// </summary>
    internal static class ThreadSafeRandom
    {
        private static int _seed = Environment.TickCount;
        private static readonly ThreadLocal<Random> Random = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref _seed)));

        internal static int Next()
        {
            return Random.Value.Next();
        }

        internal static int Next(int maxValue)
        {
            return Random.Value.Next(maxValue);
        }

        internal static int Next(int minValue, int maxValue)
        {
            return Random.Value.Next(minValue, maxValue);
        }
    }
}
