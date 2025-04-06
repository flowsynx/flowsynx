using FlowSynx.Application.Services;
using FlowSynx.Infrastructure.PluginHost;
using FlowSynx.PluginCore;
using Moq;

namespace FlowSynx.Infrastructure.UnitTests.Services;

public class PluginSpecificationsServiceTests
{
    private readonly Mock<IPluginService> _pluginServiceMock;
    private readonly PluginSpecificationsService _service;

    public PluginSpecificationsServiceTests()
    {
        _pluginServiceMock = new Mock<IPluginService>();
        _service = new PluginSpecificationsService(_pluginServiceMock.Object);
    }

    [Fact]
    public async Task Validate_ShouldReturnValid_WhenNoRequiredProperties()
    {
        // Arrange
        var plugin = new NoRequiredPropertiesPlugin();
        _pluginServiceMock.Setup(s => s.Get(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(plugin);

        var specifications = new Dictionary<string, object?>
        {
            { "someProperty", "someValue" }
        };

        // Act
        var result = await _service.Validate("TestPlugin", specifications, CancellationToken.None);

        // Assert
        Assert.True(result.Valid);
    }

    [Fact]
    public async Task Validate_ShouldReturnInvalid_WhenRequiredPropertyIsMissing()
    {
        // Arrange
        var plugin = new RequiredPropertiesPlugin();
        _pluginServiceMock.Setup(s => s.Get(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(plugin);

        var specifications = new Dictionary<string, object?>();

        // Act
        var result = await _service.Validate("TestPlugin", specifications, CancellationToken.None);

        // Assert
        Assert.False(result.Valid);
        Assert.Contains("RequiredProperty", result.Message);
    }

    [Fact]
    public async Task Validate_ShouldReturnInvalid_WhenRequiredPropertyIsNull()
    {
        // Arrange
        var plugin = new RequiredPropertiesPlugin();
        _pluginServiceMock.Setup(s => s.Get(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(plugin);

        var specifications = new Dictionary<string, object?>
        {
            { "RequiredProperty", null }
        };

        // Act
        var result = await _service.Validate("TestPlugin", specifications, CancellationToken.None);

        // Assert
        Assert.False(result.Valid);
        Assert.Contains("RequiredProperty", result.Message);
    }

    private class NoRequiredPropertiesClass : PluginSpecifications
    {
        public string SomeProperty { get; set; } = string.Empty;
    }

    private class RequiredPropertyClass: PluginSpecifications
    {
        [RequiredMember]
        public string RequiredProperty { get; set; } = string.Empty;
    }

    private class NoRequiredPropertiesPlugin : Plugin
    {
        public override Guid Id => Guid.Parse("D30035A6-EE7F-4B2C-BE24-91E57811B325");
        public override string Name => "NoRequiredPropertiesPlugin";
        public override PluginNamespace Namespace => PluginNamespace.Connectors;
        public override string? Description => "NoRequiredPropertiesPlugin";
        public override PluginSpecifications? Specifications { get; set; }
        public override Type SpecificationsType => typeof(NoRequiredPropertiesClass);

        public override Task<object?> ExecuteAsync(PluginParameters parameters, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task Initialize()
        {
            throw new NotImplementedException();
        }
    }

    private class RequiredPropertiesPlugin : Plugin
    {
        public override Guid Id => Guid.Parse("CE5024A8-D616-40FF-BA30-71D94A4A8FC8");
        public override string Name => "RequiredPropertiesPlugin";
        public override PluginNamespace Namespace => PluginNamespace.Connectors;
        public override string? Description => "RequiredPropertiesPlugin";
        public override PluginSpecifications? Specifications { get; set; }
        public override Type SpecificationsType => typeof(RequiredPropertyClass);

        public override Task<object?> ExecuteAsync(PluginParameters parameters, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task Initialize()
        {
            throw new NotImplementedException();
        }
    }
}
