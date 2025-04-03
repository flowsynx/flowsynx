using Moq;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging.Testing;
using FlowSynx.Infrastructure.Services;

namespace FlowSynx.Infrastructure.UnitTests.Services;

public class JsonSerializerTests
{
    private readonly FakeLogger<JsonSerializer> _logger;

    public JsonSerializerTests()
    {
        _logger = new FakeLogger<JsonSerializer>();
    }

    [Fact]
    public void Serialize_ShouldThrowFlowSynxException_WhenInputIsNull()
    {
        // Arrange
        var jsonSerializer = new JsonSerializer(_logger);
        object? input = null;

        // Act & Assert
        var exception = Assert.Throws<FlowSynxException>(() => jsonSerializer.Serialize(input));

        // Assert
        Assert.Equal((int)ErrorCode.Serialization, exception.ErrorCode);
        Assert.Equal("Input value can't be empty or null.", exception.Message);
        Assert.Contains(_logger.Collector.GetSnapshot(), e => e.Level == LogLevel.Error);
    }

    [Fact]
    public void Serialize_ShouldReturnsSerializedString_WhenValidInput()
    {
        // Arrange
        var jsonSerializer = new JsonSerializer(_logger);
        var input = new Person { Name = "Amin Ziagham", Age = 30 };

        // Act
        var result = jsonSerializer.Serialize(input);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("\"Name\":\"Amin Ziagham\"", result);
        Assert.Contains("\"Age\":30", result);
    }

    [Fact]
    public void Serialize_ShouldReturnsIndentedJson_WhenWithIndentedConfiguration()
    {
        // Arrange
        var jsonSerializer = new JsonSerializer(_logger);
        var input = new Person { Name = "Amin Ziagham", Age = 30 };
        var configuration = new JsonSerializationConfiguration { Indented = true };

        // Act
        var result = jsonSerializer.Serialize(input, configuration);

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
        var jsonSerializer = new JsonSerializer(_logger);
        var invalidInput = new UnserializableClass { SomeAction = () => Console.WriteLine("Hello") };

        // Act & Assert
        var exception = Assert.Throws<FlowSynxException>(() => jsonSerializer.Serialize(invalidInput));

        Assert.Equal((int)ErrorCode.Serialization, exception.ErrorCode);
        Assert.Contains(_logger.Collector.GetSnapshot(), e => e.Level == LogLevel.Error);
    }

    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    public class UnserializableClass
    {
        public Action? SomeAction { get; set; } // Action cannot be serialized
    }
}
