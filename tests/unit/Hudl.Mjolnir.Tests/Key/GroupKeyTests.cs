using System;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Tests.Helper;
using Xunit;

namespace Hudl.Mjolnir.Tests.Key
{
    public class GroupKeyTests : TestFixture
    {
        [Fact]
        public void Equals_WhenNamesEqual_ReturnsTrue()
        {
            Assert.Equal(GroupKey.Named("Foo"), GroupKey.Named("Foo"));
        }

        [Fact]
        public void Equals_WhenNamesDiffer_ReturnsFalse()
        {
            Assert.NotEqual(GroupKey.Named("Foo"), GroupKey.Named("Bar"));
        }

        [Fact]
        public void GetHashCode_ReturnsNameHashCode()
        {
            Assert.Equal("Foo".GetHashCode(), GroupKey.Named("Foo").GetHashCode());
        }

        [Fact]
        public void ToString_ReturnsName()
        {
            Assert.Equal("Foo", GroupKey.Named("Foo").ToString());
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Construct_ValidationInvalid(string name)
        {
            Assert.Throws<ArgumentException>(() => GroupKey.Named(name));
        }

        [Theory]
        [InlineData("a")]
        [InlineData("A")]
        [InlineData("0")]
        [InlineData("_")]
        [InlineData(".")]
        [InlineData("_ab")]
        [InlineData("0ab")]
        [InlineData("abc.")]
        [InlineData("abc#")]
        [InlineData("abc-")]
        [InlineData("abc$")]
        [InlineData("abc/")]
        [InlineData("abc")]
        [InlineData("Abc")]
        [InlineData("ABC")]
        [InlineData("o_o")]
        public void Construct_ValidationValid(string name)
        {
            GroupKey.Named(name);
            Assert.True(true); // Didn't throw exception.
        }
    }
}
