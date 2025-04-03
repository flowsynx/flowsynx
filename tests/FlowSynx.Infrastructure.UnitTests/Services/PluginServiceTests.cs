using FlowSynx.Infrastructure.Services;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.PluginCore;
using Moq;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Models;

namespace FlowSynx.Infrastructure.UnitTests.Services;

public class PluginServiceTests
{
    private readonly FakeLogger<PluginService> _logger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly PluginService _pluginService;

    public PluginServiceTests()
    {
        _logger = new FakeLogger<PluginService>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _pluginService = new PluginService(_logger, _mockServiceProvider.Object);
    }

    [Fact]
    public async Task All_ShouldReturnAllPlugins()
    {
        // Arrange
        var plugin = new TestPlugin();
        var plugins = new List<Plugin> { plugin };
        _mockServiceProvider.Setup(x => x.GetService(typeof(IEnumerable<Plugin>))).Returns(plugins);

        // Act
        var result = await _pluginService.All(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task Get_ById_ShouldReturnPlugin_WhenFound()
    {
        // Arrange
        var plugin = new TestPlugin();
        var plugins = new List<Plugin> { plugin };
        _mockServiceProvider.Setup(x => x.GetService(typeof(IEnumerable<Plugin>))).Returns(plugins);

        // Act
        var result = await _pluginService.Get(plugin.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<TestPlugin>(result);
    }

    [Fact]
    public async Task Get_ById_ShouldThrowException_WhenNotFound()
    {
        // Arrange
        var plugins = new List<Plugin> { };
        _mockServiceProvider.Setup(x => x.GetService(typeof(IEnumerable<Plugin>))).Returns(plugins);
        var pluginId = Guid.Parse("500852E4-F03D-48B8-9A2A-C8F00668F0E4");

        // Act
        var exception = await Assert.ThrowsAsync<FlowSynxException>(async () =>
            await _pluginService.Get(pluginId, CancellationToken.None));

        // Assert
        var message = $"Plugin with id '{pluginId}' could not found!";
        Assert.Equal((int)ErrorCode.PluginNotFound, exception.ErrorCode);
        Assert.Contains(message, exception.Message);
        Assert.Contains(_logger.Collector.GetSnapshot(), e => e.Level == LogLevel.Error && e.Message.Contains(message));
    }

    [Fact]
    public async Task Get_ByType_ShouldReturnPlugin_WhenFound()
    {
        // Arrange
        var plugin = new TestPlugin();
        var plugins = new List<Plugin> { plugin };
        _mockServiceProvider.Setup(x => x.GetService(typeof(IEnumerable<Plugin>))).Returns(plugins);

        // Act
        var result = await _pluginService.Get(plugin.Type, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<TestPlugin>(result);
    }

    [Fact]
    public async Task Get_ByType_ShouldThrowException_WhenNotFound()
    {
        // Arrange
        var plugins = new List<Plugin> { };
        _mockServiceProvider.Setup(x => x.GetService(typeof(IEnumerable<Plugin>))).Returns(plugins);

        // Act
        var exception = await Assert.ThrowsAsync<FlowSynxException>(async () =>
            await _pluginService.Get("NonExistentPlugin", CancellationToken.None));

        // Assert
        Assert.Equal((int)ErrorCode.PluginTypeNotFound, exception.ErrorCode);
        Assert.Contains("Plugin NonExistentPlugin could not found!", exception.Message);
        Assert.Contains(_logger.Collector.GetSnapshot(), e => e.Level == LogLevel.Error && e.Message.Contains("Plugin NonExistentPlugin could not found!"));
    }

    [Fact]
    public async Task IsExist_ShouldReturnTrue_WhenPluginExists()
    {
        // Arrange
        var plugin = new TestPlugin();
        var plugins = new List<Plugin> { plugin };
        _mockServiceProvider.Setup(x => x.GetService(typeof(IEnumerable<Plugin>))).Returns(plugins);


        // Act
        var result = await _pluginService.IsExist(plugin.Type, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsExist_ShouldReturnFalse_WhenPluginDoesNotExist()
    {
        // Arrange
        var plugins = new List<Plugin> { };
        _mockServiceProvider.Setup(x => x.GetService(typeof(IEnumerable<Plugin>))).Returns(plugins);

        // Act
        var result = await _pluginService.IsExist("NonExistentPlugin", CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnTrue_WhenPluginsCanBeRetrieved()
    {
        // Arrange
        var plugin = new TestPlugin();
        var plugins = new List<Plugin> { plugin };
        _mockServiceProvider.Setup(x => x.GetService(typeof(IEnumerable<Plugin>))).Returns(plugins);

        // Act
        var result = await _pluginService.CheckHealthAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnFalse_WhenExceptionOccurs()
    {
        // Arrange
        _mockServiceProvider.Setup(x => x.GetService(typeof(IEnumerable<Plugin>))).Throws(new Exception());

        // Act
        var result = await _pluginService.CheckHealthAsync(CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    private class TestPlugin : Plugin
    {
        public override Guid Id => Guid.Parse("D30035A6-EE7F-4B2C-BE24-91E57811B325");

        public override string Name => "TestPlugin";

        public override PluginNamespace Namespace => PluginNamespace.Connectors;

        public override string? Description => "TestPlugin";

        public override PluginSpecifications? Specifications { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override Type SpecificationsType => throw new NotImplementedException();

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