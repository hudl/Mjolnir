using Hudl.Mjolnir.Key;
using System;

namespace Hudl.Mjolnir.Tests.Helper
{
    public class TestFixture
    {
        protected static readonly GroupKey AnyGroupKey = GroupKey.Named(AnyString);

        protected static string AnyString
        {
            get { return Rand.String(); }
        }

        protected static int AnyPositiveInt
        {
            get { return Rand.PositiveInt(); }
        }
        
        protected static bool AnyBool
        {
            get { return Rand.Bool(); }
        }

        protected static TimeSpan AnyPositiveTimeSpan
        {
            get { return TimeSpan.FromMilliseconds(AnyPositiveInt); }
        }
    }
}
