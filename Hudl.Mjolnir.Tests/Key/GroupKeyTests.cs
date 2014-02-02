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


        [Fact]
        public void Construct_Validation()
        {
            AssertInvalidName(""); // Cannot be empty.
            AssertInvalidName(" "); // Cannot be empty.
            AssertInvalidName(null); // Cannot be null.

            AssertValidName("a");
            AssertValidName("A");
            AssertValidName("0");
            AssertValidName("_");
            AssertValidName(".");
            AssertValidName("_ab");
            AssertValidName("0ab");
            AssertValidName("abc.");
            AssertValidName("abc#");
            AssertValidName("abc-");
            AssertValidName("abc$");
            AssertValidName("abc/");
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
