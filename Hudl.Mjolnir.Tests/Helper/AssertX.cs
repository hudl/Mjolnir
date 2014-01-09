using Xunit;

namespace Hudl.Mjolnir.Tests.Helper
{
    public static class AssertX
    {
        // TODO rob.hruska 11/4/2013 - Once we upgrade to xunit 2.0.0 we can probably
        // get rid of this and use the ExpectedException attribute. It currently doesn't
        // work pre-2.0.0 for async methods. 2.0.0 is prerelease, and was giving me
        // some problems when I tried to use it, so I stuck with 1.9.1 for now and used
        // this Fail() helper method instead.
        public static void FailExpectedException()
        {
            Assert.True(false, "Expected Exception");
        }
    }
}