using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Behaviors;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;

namespace FlowSynx.Application.UnitTests;

public class PerformanceBehaviorTests
{
    private readonly FakeLogger<PerformanceBehavior<TestRequest, TestResponse>> _logger;
    private readonly PerformanceBehavior<TestRequest, TestResponse> _behavior;
    private readonly Mock<RequestHandlerDelegate<TestResponse>> _nextMock;

    public PerformanceBehaviorTests()
    {
        _logger = new FakeLogger<PerformanceBehavior<TestRequest, TestResponse>>();
        _behavior = new PerformanceBehavior<TestRequest, TestResponse>(_logger);
        _nextMock = new Mock<RequestHandlerDelegate<TestResponse>>();
    }

    [Fact]
    public async Task Handle_WhenRequestTakesLongerThanThreshold_LogsWarning()
    {
        // Arrange
        var testRequest = new TestRequest();
        var testResponse = new TestResponse();

        _nextMock.Setup(x => x()).ReturnsAsync(testResponse);

        var cancellationToken = CancellationToken.None;
        var delayTime = 1500;

        _nextMock.Setup(x => x()).Returns(async () => { 
            await Task.Delay(delayTime); 
            return testResponse; 
        });

        // Act
        await _behavior.Handle(testRequest, _nextMock.Object, cancellationToken);

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

        _nextMock.Setup(x => x()).ReturnsAsync(testResponse);

        // Act
        await _behavior.Handle(testRequest, _nextMock.Object, CancellationToken.None);

        // Assert
        Assert.Empty(_logger.Collector.GetSnapshot());
    }

    public class TestRequest : IRequest<TestResponse> { }

    public class TestResponse { }
}