using System;
using Hudl.Mjolnir.Key;
using Xunit;

namespace Hudl.Mjolnir.Tests.Key
{
    public class GroupKeyTests
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

        [Fact]
        public void Construct_WithNullName_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                GroupKey.Named(null);
            });
        }

        [Fact]
        public void Construct_Validation()
        {
            AssertInvalidName(""); // Must be at least three chars
            AssertInvalidName("a"); // Must be at least three chars
            AssertInvalidName("A"); // Must be at least three chars
            AssertInvalidName("0"); // Must be at least three chars
            AssertInvalidName("_"); // Must be at least three chars
            AssertInvalidName("."); // Must be at least three chars
            AssertInvalidName("_ab"); // Must start with a letter
            AssertInvalidName("0ab"); // Must start with a letter
            AssertInvalidName("abc."); // Cannot contain invalid chars
            AssertInvalidName("abc#"); // Cannot contain invalid chars
            AssertInvalidName("abc-"); // Cannot contain invalid chars
            AssertInvalidName("abc$"); // Cannot contain invalid chars
            AssertInvalidName("abc/"); // Cannot contain invalid chars

            AssertValidName("abc");
            AssertValidName("Abc");
            AssertValidName("ABC");
            AssertValidName("o_o");
        }

        private static void AssertValidName(string name)
        {
            GroupKey.Named(name);
            Assert.True(true); // Didn't throw exception.
        }

        private static void AssertInvalidName(string name)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                GroupKey.Named(name);
            });
        }
    }
}
