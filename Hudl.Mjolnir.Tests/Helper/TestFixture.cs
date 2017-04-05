using Hudl.Mjolnir.Key;

namespace Hudl.Mjolnir.Tests.Helper
{
    public class TestFixture
    {
        protected static readonly GroupKey AnyGroupKey = GroupKey.Named(AnyString);

        protected static string AnyString
        {
            get { return Rand.String(); }
        }

        protected static int AnyInt
        {
            get { return Rand.Int(); }
        }

        protected static long AnyLong
        {
            get { return Rand.Long(); }
        }

        protected static bool AnyBool
        {
            get { return Rand.Bool(); }
        }
    }
}
