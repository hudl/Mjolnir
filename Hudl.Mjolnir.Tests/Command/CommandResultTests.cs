using System;
using Hudl.Mjolnir.Command;
using Hudl.Mjolnir.Tests.Helper;
using Xunit;

namespace Hudl.Mjolnir.Tests.Command
{
    public class CommandResultTests
    {
        [Fact]
        public void WhenUnsuccessfulAndValueRetrieved_Throws()
        {
            var result = new CommandResult<object>(new { }, new Exception());
            Assert.Throws<InvalidOperationException>(() => result.Value);
        }

        [Fact]
        public void WhenUnsuccessful_HasPopulatedException()
        {
            var exception = new ExpectedTestException("expected");
            var result = new CommandResult<object>(new { }, exception);
            Assert.Equal(exception, result.Exception);
        }

        [Fact]
        public void WhenSuccessful_HasNoException()
        {
            var result = new CommandResult<object>(new { });
            Assert.Null(result.Exception);
        }

        [Fact]
        public void WhenSuccessful_HasValue()
        {
            var expected = new { };
            var result = new CommandResult<object>(expected);
            Assert.Equal(expected, result.Value);
        }
    }
}
