using FlowSynx.Domain.Wrapper;

namespace FlowSynx.Application.UnitTests.Wrapper;

public class ResultGenericTests
{
    [Fact]
    public void Fail_Generic_ShouldReturnFailedResult()
    {
        var result = Result<int>.Fail();
        Assert.False(result.Succeeded);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public void Success_Generic_ShouldReturnSuccessfulResultWithData()
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