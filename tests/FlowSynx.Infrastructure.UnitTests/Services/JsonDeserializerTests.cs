using FlowSynx.Application.Models;
using FlowSynx.Infrastructure.Services;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace FlowSynx.Infrastructure.UnitTests.Services;

public class JsonDeserializerTests
{
    private readonly FakeLogger<JsonDeserializer> _logger;

    private readonly JsonDeserializer _jsonDeserializer;

    public JsonDeserializerTests()
    {
        _logger = new FakeLogger<JsonDeserializer>();
        _jsonDeserializer = new JsonDeserializer(_logger);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new JsonDeserializer(null!));
    }

    [Fact]
    public void Deserialize_ShouldThrowFlowSynxException_WhenInputIsNull()
    {
        // Arrange
        string? input = null;

        // Act
        var exception = Assert.Throws<FlowSynxException>(() => _jsonDeserializer.Deserialize<object>(input));

        // Assert
        Assert.Equal((int)ErrorCode.Serialization, exception.ErrorCode);
        Assert.Equal("Input value can't be empty or null.", exception.Message);
        Assert.Contains(_logger.Collector.GetSnapshot(), e => e.Level == LogLevel.Error && e.Message.Contains("Input value can't be empty or null."));
    }

    [Fact]
    public void Deserialize_ShouldThrowFlowSynxException_WhenInputIsEmpty()
    {
        // Arrange
        string input = string.Empty;

        // Act
        var exception = Assert.Throws<FlowSynxException>(() => _jsonDeserializer.Deserialize<object>(input));

        // Assert
        Assert.Equal((int)ErrorCode.Serialization, exception.ErrorCode);
        Assert.Equal("Input value can't be empty or null.", exception.Message);
        Assert.Contains(_logger.Collector.GetSnapshot(), e => e.Level == LogLevel.Error && e.Message.Contains("Input value can't be empty or null."));
    }

    [Fact]
    public void Deserialize_ShouldDeserializeObject_WhenValidJson()
    {
        // Arrange
        string input = "{\"name\":\"Amin Ziagham\",\"age\":30}";

        // Act
        var result = _jsonDeserializer.Deserialize<Person>(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Amin Ziagham", result.Name);
        Assert.Equal(30, result.Age);
    }

    [Fact]
    public void Deserialize_ShouldApplyIndentedFormatting_WhenConfigurationIsSetToIndented()
    {
        // Arrange
        string input = "{\"name\":\"Amin Ziagham\",\"age\":30}";
        var config = new JsonSerializationConfiguration { Indented = true };

        // Act
        var result = _jsonDeserializer.Deserialize<Person>(input, config);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Amin Ziagham", result.Name);
        Assert.Equal(30, result.Age);
    }

    [Fact]
    public void Deserialize_ShouldHandleSerializationException_WhenJsonIsInvalid()
    {
        // Arrange
        string invalidJson = "{\"name\":\"Amin Ziagham\", \"age\":}";

        // Act
        var exception = Assert.Throws<FlowSynxException>(() => _jsonDeserializer.Deserialize<Person>(invalidJson));

        // Assert
        Assert.Equal((int)ErrorCode.Serialization, exception.ErrorCode);
        Assert.Contains("Unexpected character encountered while parsing value: }. Path 'age'", exception.Message);
        Assert.Contains(_logger.Collector.GetSnapshot(), e => e.Level == LogLevel.Error && e.Message.Contains("Unexpected character encountered while parsing value: }. Path 'age"));
    }

    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}