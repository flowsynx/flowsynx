using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Behaviors;
using Microsoft.Extensions.Logging.Testing;

namespace FlowSynx.Application.UnitTests;

public class PerformanceBehaviorTests
{
    private readonly FakeLogger<PerformanceBehavior<TestRequest, TestResponse>> _logger;
    private readonly PerformanceBehavior<TestRequest, TestResponse> _behavior;

    public PerformanceBehaviorTests()
    {
        _logger = new FakeLogger<PerformanceBehavior<TestRequest, TestResponse>>();
        _behavior = new PerformanceBehavior<TestRequest, TestResponse>(_logger);
    }

    [Fact]
    public async Task Handle_WhenRequestTakesLongerThanThreshold_LogsWarning()
    {
        // Arrange
        var testRequest = new TestRequest();
        var testResponse = new TestResponse();

        var cancellationToken = CancellationToken.None;
        var delayTime = 1500;

        RequestHandlerDelegate<TestResponse> next = async (x) =>
        {
            await Task.Delay(delayTime);
            return testResponse;
        };

        // Act
        await _behavior.Handle(testRequest, next, cancellationToken);

        // Assert
        Assert.NotEmpty(_logger.Collector.GetSnapshot());
        Assert.Contains(_logger.Collector.GetSnapshot(), e => e.Level == LogLevel.Warning && e.Message.Contains("took"));
    }

    [Fact]
    public async Task Handle_WhenRequestCompletesNormally_LogsNoWarnings()
    {
        // Arrange
        var testRequest = new TestRequest();
        var testResponse = new TestResponse();
        RequestHandlerDelegate<TestResponse> next = (x) => Task.FromResult(testResponse);

        // Act
        await _behavior.Handle(testRequest, next, CancellationToken.None);

        // Assert
        Assert.Empty(_logger.Collector.GetSnapshot());
    }

    public class TestRequest : IRequest<TestResponse> { }

    public class TestResponse { }
}