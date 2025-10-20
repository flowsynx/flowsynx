using FlowSynx.Application.Extensions;
using FluentValidation;

namespace FlowSynx.Application.UnitTests.Extensions;

public class ValidatorExtensionsTests
{
    private const string ErrorMessage = "Invalid GUID";

    [Fact]
    public void MustBeValidGuid_ValidGuid_PassesValidation()
    {
        // Arrange
        var validator = new TestValidator(ErrorMessage);
        var request = new TestRequest { Id = Guid.NewGuid().ToString() };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void MustBeValidGuid_InvalidGuid_FailsWithProvidedMessage()
    {
        // Arrange
        var validator = new TestValidator(ErrorMessage);
        var request = new TestRequest { Id = "not-a-guid" };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorMessage, error.ErrorMessage);
    }

    [Fact]
    public void MustBeValidGuid_NullMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new TestValidator(null!));
        Assert.Equal("message", exception.ParamName);
    }

    private sealed class TestRequest
    {
        public string Id { get; set; } = string.Empty;
    }

    private sealed class TestValidator : AbstractValidator<TestRequest>
    {
        public TestValidator(string message)
        {
            RuleFor(x => x.Id)
                .MustBeValidGuid(message);
        }
    }
}

