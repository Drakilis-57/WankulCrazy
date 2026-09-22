using System;
using Xunit;
using WankulCrazyPlugin.utils.obj;

namespace WankulCrazyPlugin.Tests
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData("hello world", "hello world")]
        [InlineData("hello\tworld", "hello world")]
        [InlineData("hello\t\tworld", "hello world")]
        [InlineData("hello   world", "hello world")]
        [InlineData("hello \t  world", "hello world")]
        [InlineData("  hello world  ", "hello world")]
        [InlineData("\thello world\t", "hello world")]
        [InlineData("  \t hello \t world \t ", "hello world")]
        [InlineData("", "")]
        [InlineData("   \t  ", "")]
        public void Clean_ShouldCleanWhitespaceAndTabsCorrectly(string input, string expected)
        {
            // Act
            string result = input.Clean();

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Clean_NullInput_ThrowsNullReferenceException()
        {
            // Arrange
            string nullString = null!;

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => nullString.Clean());
        }
    }
}
