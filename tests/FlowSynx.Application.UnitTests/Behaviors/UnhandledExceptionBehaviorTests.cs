using Microsoft.Extensions.Logging;
using MediatR;
using FlowSynx.Application.Behaviors;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace FlowSynx.Application.UnitTests.Behaviors;

public class UnhandledExceptionBehaviorTests
{
    private readonly FakeLogger<UnhandledExceptionBehavior<TestRequest, TestResponse>> _logger;
    private readonly UnhandledExceptionBehavior<TestRequest, TestResponse> _behavior;
    private readonly Mock<RequestHandlerDelegate<TestResponse>> _nextMock;

    public UnhandledExceptionBehaviorTests()
    {
        _logger = new FakeLogger<UnhandledExceptionBehavior<TestRequest, TestResponse>>();
        _behavior = new UnhandledExceptionBehavior<TestRequest, TestResponse>(_logger);
        _nextMock = new Mock<RequestHandlerDelegate<TestResponse>>();
    }

    [Fact]
    public async Task Handle_ShouldLogException_WhenExceptionOccurs()
    {
        // Arrange
        var request = new TestRequest();
        var cancellationToken = CancellationToken.None;

        var exception = new InvalidOperationException("Test exception");
        _nextMock.Setup(x => x()).ThrowsAsync(exception);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _behavior.Handle(request, _nextMock.Object, cancellationToken));

        // Assert
        Assert.Contains(_logger.Collector.GetSnapshot(), e => e.Level == LogLevel.Error && e.Message.Contains("Exception occurred in request"));
    }

    [Fact]
    public async Task Handle_ShouldReturnResponse_WhenNoExceptionOccurs()
    {
        // Arrange
        var request = new TestRequest();
        var testResponse = new TestResponse();
        var cancellationToken = CancellationToken.None;

        _nextMock.Setup(x => x()).ReturnsAsync(testResponse);

        // Act
        var response = await _behavior.Handle(request, _nextMock.Object, cancellationToken);

        // Assert
        Assert.NotNull(response);
    }

    // Test request and response classes for testing
    public class TestRequest : IRequest<TestResponse>
    {
    }

    public class TestResponse
    {
    }
}
