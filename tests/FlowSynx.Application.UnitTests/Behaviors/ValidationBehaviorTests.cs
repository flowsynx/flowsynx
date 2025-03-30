using FluentValidation.Results;
using FluentValidation;
using MediatR;
using Moq;
using FlowSynx.Application.Behaviors;
using FlowSynx.Application.Exceptions;
using FlowSynx.PluginCore.Exceptions;

namespace FlowSynx.Application.UnitTests.Behaviors;

public class ValidationBehaviorTests
{
    private readonly Mock<IValidator<TestRequest>> _mockValidator;
    private readonly ValidationBehavior<TestRequest, TestResponse> _validationBehavior;
    private readonly Mock<RequestHandlerDelegate<TestResponse>> _mockNext;

    public ValidationBehaviorTests()
    {
        _mockValidator = new Mock<IValidator<TestRequest>>();
        _validationBehavior = new ValidationBehavior<TestRequest, TestResponse>(_mockValidator.Object);
        _mockNext = new Mock<RequestHandlerDelegate<TestResponse>>();
    }

    [Fact]
    public async Task Handle_Should_CallNext_When_ValidRequest()
    {
        // Arrange
        var request = new TestRequest();
        var response = new TestResponse();

        var validationResult = new ValidationResult();
        _mockValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(validationResult);

        _mockNext.Setup(n => n()).ReturnsAsync(response);

        // Act
        var result = await _validationBehavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        _mockValidator.Verify(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockNext.Verify(n => n(), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowValidationException_When_InvalidRequest()
    {
        // Arrange
        var request = new TestRequest();
        var validationErrors = new List<ValidationFailure>
        {
            new ValidationFailure("Property", "Validation error")
        };
        var validationResult = new ValidationResult(validationErrors);

        _mockValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(validationResult);

        // Act & Assert
        await Assert.ThrowsAsync<FlowSynxException>(() => _validationBehavior.Handle(request, _mockNext.Object, CancellationToken.None));
        _mockValidator.Verify(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockNext.Verify(n => n(), Times.Never);
    }

    // Test request and response classes for testing
    public class TestRequest : IRequest<TestResponse>
    {
    }

    public class TestResponse
    {
    }
}