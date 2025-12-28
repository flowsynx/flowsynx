using FlowSynx.Application.Localizations;
using FlowSynx.Application.Serialization;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using FlowSynx.Domain.Primitives;
using FlowSynx.Infrastructure.Serializations.NewtonsoftJson;

namespace FlowSynx.Infrastructure.UnitTests.Serialization;

public class JsonDeserializerTests
{
    private readonly FakeLogger<JsonDeserializer> _logger;
    private readonly Mock<ILocalization> localizationMock;
    private readonly JsonSanitizer _jsonSanitizer;
    private readonly JsonDeserializer _jsonDeserializer;

    public JsonDeserializerTests()
    {
        _logger = new FakeLogger<JsonDeserializer>();
        localizationMock = new Mock<ILocalization>();
        localizationMock.Setup(l => l.Get("JsonDeserializer_InputValueCanNotBeEmpty")).Returns("Input value can't be empty or null.");
        _jsonSanitizer = new JsonSanitizer();
        _jsonDeserializer = new JsonDeserializer(_logger, _jsonSanitizer, localizationMock.Object);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new JsonDeserializer(null!, _jsonSanitizer, localizationMock.Object));
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
        Assert.Contains(_logger.Collector.GetSnapshot(), 
            e => e.Level == LogLevel.Error && e.Message.Contains("Input value can't be empty or null."));
    }

    [Fact]
    public void Deserialize_ShouldThrowFlowSynxException_WhenInputIsEmpty()
    {
        // Arrange
        var input = string.Empty;

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
        var input = "{\"name\":\"Amin Ziagham\",\"age\":30}";

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
        var input = "{\"name\":\"Amin Ziagham\",\"age\":30}";
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
        var invalidJson = "{\"name\":\"Amin Ziagham\", \"age\":}";

        // Act
        var exception = Assert.Throws<FlowSynxException>(() => _jsonDeserializer.Deserialize<Person>(invalidJson));

        // Assert
        Assert.Equal((int)ErrorCode.Serialization, exception.ErrorCode);
        Assert.Contains("Unexpected character encountered while parsing value: }. Path 'age'", exception.Message);
        Assert.Contains(_logger.Collector.GetSnapshot(), e => e.Level == LogLevel.Error && e.Message.Contains("Unexpected character encountered while parsing value: }. Path 'age"));
    }

    public class Person
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }
}