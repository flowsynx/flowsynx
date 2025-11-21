using FlowSynx.Domain.Wrapper;

namespace FlowSynx.Domain.UnitTests.Wrapper;

public class ResultGenericTests
{
    [Fact]
    public void Fail_WithNoParameters_ShouldReturnFailedResult()
    {
        // Act
        var result = Result<string>.Fail();

        // Assert
        Assert.False(result.Succeeded);
        Assert.Empty(result.Messages);
        Assert.Null(result.Data);
    }

    [Fact]
    public void Fail_WithMessage_ShouldReturnFailedResultWithMessage()
    {
        // Arrange
        var message = "Operation failed";

        // Act
        var result = Result<int>.Fail(message);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Messages);
        Assert.Contains(message, result.Messages);
        Assert.Equal(0, result.Data);
    }

    [Fact]
    public void Fail_WithMultipleMessages_ShouldReturnFailedResultWithAllMessages()
    {
        // Arrange
        var messages = new List<string> { "Error 1", "Error 2" };

        // Act
        var result = Result<bool>.Fail(messages);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(messages, result.Messages);
    }

    [Fact]
    public async Task FailAsync_WithNoParameters_ShouldReturnFailedResult()
    {
        // Act
        var result = await Result<string>.FailAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public async Task FailAsync_WithMessage_ShouldReturnFailedResultWithMessage()
    {
        // Arrange
        var message = "Async operation failed";

        // Act
        var result = await Result<double>.FailAsync(message);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(message, result.Messages);
    }

    [Fact]
    public async Task FailAsync_WithMultipleMessages_ShouldReturnFailedResultWithAllMessages()
    {
        // Act
        var result = await Result<string>.FailAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public void         Success_Generic_ShouldReturnSuccessfulResultWithData()
    {
        var result = Result<int>.Success(42);
        Assert.True(result.Succeeded);
        Assert.Equal(42, result.Data);
    }

    [Fact]
    public async Task SuccessAsync_Generic_ShouldReturnSuccessfulResultWithData()
    {
        var result = await Result<int>.SuccessAsync(100);
        Assert.True(result.Succeeded);
        Assert.Equal(100, result.Data);
    }

    [Fact]
    public void Success_Generic_WithMessage_ShouldReturnSuccessfulResultWithMessageAndData()
    {
        var result = Result<int>.Success(99, "Value set");
        Assert.True(result.Succeeded);
        Assert.Equal(99, result.Data);
        Assert.Single(result.Messages);
        Assert.Contains("Value set", result.Messages);
    }
}