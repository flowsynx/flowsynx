using FlowSynx.Application.Wrapper;

namespace FlowSynx.Application.UnitTests.Wrapper;

public class ResultTests
{
    [Fact]
    public void Fail_ShouldReturnFailedResult()
    {
        var result = Result.Fail();
        Assert.False(result.Succeeded);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public void Fail_WithMessage_ShouldReturnFailedResultWithMessage()
    {
        var result = Result.Fail("Error occurred");
        Assert.False(result.Succeeded);
        Assert.Single(result.Messages);
        Assert.Contains("Error occurred", result.Messages);
    }

    [Fact]
    public void Fail_WithMessages_ShouldReturnFailedResultWithMessages()
    {
        var messages = new List<string> { "Error1", "Error2" };
        var result = Result.Fail(messages);
        Assert.False(result.Succeeded);
        Assert.Equal(messages, result.Messages);
    }

    [Fact]
    public async Task FailAsync_ShouldReturnFailedResult()
    {
        var result = await Result.FailAsync();
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task FailAsync_WithMessage_ShouldReturnFailedResultWithMessage()
    {
        var result = await Result.FailAsync("Test async error");
        Assert.False(result.Succeeded);
        Assert.Single(result.Messages);
        Assert.Contains("Test async error", result.Messages);
    }

    [Fact]
    public void Success_ShouldReturnSuccessfulResult()
    {
        var result = Result.Success();
        Assert.True(result.Succeeded);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public void Success_WithMessage_ShouldReturnSuccessfulResultWithMessage()
    {
        var result = Result.Success("Operation successful");
        Assert.True(result.Succeeded);
        Assert.Single(result.Messages);
        Assert.Contains("Operation successful", result.Messages);
    }

    [Fact]
    public async Task SuccessAsync_WithMessage_ShouldReturnSuccessfulResultWithMessage()
    {
        var result = await Result.SuccessAsync("Test async success");
        Assert.True(result.Succeeded);
        Assert.Single(result.Messages);
        Assert.Contains("Test async success", result.Messages);
    }
}