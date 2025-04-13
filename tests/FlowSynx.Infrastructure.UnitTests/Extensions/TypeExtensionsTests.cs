using FlowSynx.Infrastructure.Extensions;

namespace FlowSynx.Infrastructure.UnitTests.Extensions;

public class TypeExtensionsTests
{
    [Theory]
    [InlineData(typeof(string), "String")]
    [InlineData(typeof(char), "Char")]
    [InlineData(typeof(byte), "Byte")]
    [InlineData(typeof(int), "Integer")]
    [InlineData(typeof(long), "Long")]
    [InlineData(typeof(double), "Double")]
    [InlineData(typeof(float), "Float")]
    [InlineData(typeof(decimal), "Decimal")]
    [InlineData(typeof(bool), "Boolean")]
    [InlineData(typeof(object), "Object")]
    [InlineData(typeof(DateTime), "Object")] // Unknown type fallback
    public void GetPrimitiveType_ShouldReturnExpectedString(Type type, string expected)
    {
        // Act
        var result = type.GetPrimitiveType();

        // Assert
        Assert.Equal(expected, result);
    }
}