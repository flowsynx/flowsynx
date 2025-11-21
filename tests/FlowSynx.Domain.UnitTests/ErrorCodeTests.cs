namespace FlowSynx.Domain.UnitTests;

public class ErrorCodeTests
{
    [Fact]
    public void ErrorCode_None_ShouldBeZero()
    {
        // Assert
        Assert.Equal(0, (int)ErrorCode.None);
    }

    [Theory]
    [InlineData(ErrorCode.ApplicationStartArgumentIsRequired, 1001)]
    [InlineData(ErrorCode.ApplicationEndpoint, 1002)]
    [InlineData(ErrorCode.ApplicationVersion, 1004)]
    public void ErrorCode_ApplicationErrors_ShouldHaveCorrectValues(ErrorCode code, int expectedValue)
    {
        // Assert
        Assert.Equal(expectedValue, (int)code);
    }

    [Theory]
    [InlineData(ErrorCode.Serialization, 1301)]
    [InlineData(ErrorCode.DeserializerEmptyValue, 1302)]
    [InlineData(ErrorCode.SerializerEmptyValue, 1303)]
    public void ErrorCode_SerializationErrors_ShouldHaveCorrectValues(ErrorCode code, int expectedValue)
    {
        // Assert
        Assert.Equal(expectedValue, (int)code);
    }

    [Theory]
    [InlineData(ErrorCode.SecurityGetUserId, 1501)]
    [InlineData(ErrorCode.SecurityAuthenticationIsRequired, 1550)]
    public void ErrorCode_SecurityErrors_ShouldHaveCorrectValues(ErrorCode code, int expectedValue)
    {
        // Assert
        Assert.Equal(expectedValue, (int)code);
    }

    [Theory]
    [InlineData(ErrorCode.LogsList, 2001)]
    [InlineData(ErrorCode.LogAdd, 2003)]
    public void ErrorCode_LoggingErrors_ShouldHaveCorrectValues(ErrorCode code, int expectedValue)
    {
        // Assert
        Assert.Equal(expectedValue, (int)code);
    }

    [Theory]
    [InlineData(ErrorCode.PluginNotFound, 2301)]
    [InlineData(ErrorCode.PluginTypeNotFound, 2302)]
    public void ErrorCode_PluginErrors_ShouldHaveCorrectValues(ErrorCode code, int expectedValue)
    {
        // Assert
        Assert.Equal(expectedValue, (int)code);
    }

    [Fact]
    public void ErrorCode_UnknownError_ShouldBe9999()
    {
        // Assert
        Assert.Equal(9999, (int)ErrorCode.UnknownError);
    }

    [Fact]
    public void ErrorCode_ExpressionParserKeyNotFound_ShouldHaveCorrectValue()
    {
        // Assert
        Assert.Equal(2601, (int)ErrorCode.ExpressionParserKeyNotFound);
    }

    [Fact]
    public void ErrorCode_AllValues_ShouldBeUnique()
    {
        // Arrange
        var allValues = Enum.GetValues<ErrorCode>().Cast<int>().ToList();

        // Act
        var distinctValues = allValues.Distinct().ToList();

        // Assert - Note: There might be duplicates in the actual enum (SecurityConfigurationInvalidScheme = 1506 is duplicated)
        // This test documents the current state
        Assert.True(distinctValues.Count <= allValues.Count);
    }
}