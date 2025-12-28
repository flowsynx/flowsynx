using Microsoft.Extensions.Logging;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging.Testing;
using FlowSynx.Application.Serialization;
using FlowSynx.Application.Localizations;
using Moq;
using FlowSynx.Domain.Primitives;
using FlowSynx.Infrastructure.Serializations.NewtonsoftJson;

namespace FlowSynx.Infrastructure.UnitTests.Serialization;

public class JsonSerializerTests
{
    private readonly FakeLogger<JsonSerializer> _logger = new();
    private readonly Mock<ILocalization> localizationMock;
    private readonly JsonSerializer _jsonSerializer;

    public JsonSerializerTests()
    {
        localizationMock = new Mock<ILocalization>();
        localizationMock.Setup(l => l.Get("JsonSerializer_InputValueCanNotBeEmpty")).Returns("Input value can't be empty or null.");
        _jsonSerializer = new JsonSerializer(_logger, localizationMock.Object);
    }

    [Fact]
    public void Serialize_ShouldThrowFlowSynxException_WhenInputIsNull()
    {
        // Arrange
        object? input = null;

        // Act & Assert
        var exception = Assert.Throws<FlowSynxException>(() => _jsonSerializer.Serialize(input));

        // Assert
        Assert.Equal((int)ErrorCode.Serialization, exception.ErrorCode);
        Assert.Equal("Input value can't be empty or null.", exception.Message);
        Assert.Contains(_logger.Collector.GetSnapshot(), e => e.Level == LogLevel.Error);
    }

    [Fact]
    public void Serialize_ShouldReturnsSerializedString_WhenValidInput()
    {
        // Arrange
        var input = new Person { Name = "Amin Ziagham", Age = 30 };

        // Act
        var result = _jsonSerializer.Serialize(input);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("\"Name\":\"Amin Ziagham\"", result);
        Assert.Contains("\"Age\":30", result);
    }

    [Fact]
    public void Serialize_ShouldReturnsIndentedJson_WhenWithIndentedConfiguration()
    {
        // Arrange
        var input = new Person { Name = "Amin Ziagham", Age = 30 };
        var configuration = new JsonSerializationConfiguration { Indented = true };

        // Act
        var result = _jsonSerializer.Serialize(input, configuration);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("\"Name\": \"Amin Ziagham\"", result); // Should be indented
        Assert.Contains("\"Age\": 30", result);
        Assert.Contains("\n", result); // Indicates indentation
    }

    [Fact]
    public void Serialize_ShouldHandleSerializationException_WhenJsonIsInvalid()
    {
        // Arrange
        var invalidInput = new UnSerializableClass { SomeAction = () => Console.WriteLine(@"Hello") };

        // Act & Assert
        var exception = Assert.Throws<FlowSynxException>(() => _jsonSerializer.Serialize(invalidInput));

        Assert.Equal((int)ErrorCode.Serialization, exception.ErrorCode);
        Assert.Contains(_logger.Collector.GetSnapshot(), e => e.Level == LogLevel.Error);
    }

    public class Person
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    public class UnSerializableClass
    {
        public Action? SomeAction { get; set; } // Action cannot be serialized
    }
}
