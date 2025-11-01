using FlowSynx.Infrastructure.Extensions;

namespace FlowSynx.Infrastructure.UnitTests.Extensions;

public class StringExtensionsTests
{
    [Fact]
    public void Md5HashKey_ReturnsEmptyString_WhenKeyIsNull()
    {
        // Arrange
        string? input = null;

        // Act
        var result = input.Md5HashKey();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Md5HashKey_ReturnsEmptyString_WhenKeyIsEmpty()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = input.Md5HashKey();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("abc", "900150983CD24FB0D6963F7D28E17F72")]
    [InlineData("hello", "5D41402ABC4B2A76B9719D911017C592")]
    [InlineData("Hello, World!", "65A8E27D8879283831B664BD8B7F0AD4")]
    [InlineData(" ", "7215EE9C7D9DC229D2921A40E899EC5F")]
    public void Md5HashKey_ComputesExpectedHash_ForAsciiInputs(string input, string expected)
    {
        // Act
        var result = input.Md5HashKey();

        // Assert
        Assert.Equal(expected, result);
    }
}
