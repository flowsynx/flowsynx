using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlowSynx.Application.Secrets;
using FlowSynx.Infrastructure.Secrets;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace FlowSynx.Infrastructure.UnitTests.Secrets;

public class SecretConfigurationSourceTests
{
    [Fact]
    public void Constructor_DoesNotThrow_WithValidProvider()
    {
        // Arrange
        var providerMock = new Mock<ISecretProvider>(MockBehavior.Strict);

        // Act
        var source = new SecretConfigurationSource(providerMock.Object);

        // Assert
        Assert.NotNull(source);
    }

    [Fact]
    public void Build_ReturnsSecretConfigurationProvider()
    {
        // Arrange
        var providerMock = new Mock<ISecretProvider>(MockBehavior.Strict);
        var source = new SecretConfigurationSource(providerMock.Object);
        var builder = new ConfigurationBuilder();

        // Act
        var provider = source.Build(builder);

        // Assert
        Assert.NotNull(provider);
        Assert.IsType<SecretConfigurationProvider>(provider);
    }

    [Fact]
    public void Build_CanHandleNullBuilder()
    {
        // Arrange
        var providerMock = new Mock<ISecretProvider>(MockBehavior.Strict);
        var source = new SecretConfigurationSource(providerMock.Object);

        // Act
        var provider = source.Build(builder: null!);

        // Assert
        Assert.NotNull(provider);
        Assert.IsType<SecretConfigurationProvider>(provider);
    }

    [Fact]
    public void Build_ProviderUsesInjectedSecretProvider_OnLoad()
    {
        // Arrange
        var secrets = new List<KeyValuePair<string, string>>();
        var providerMock = new Mock<ISecretProvider>(MockBehavior.Strict);
        providerMock
            .Setup(p => p.GetSecretsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(secrets)
            .Verifiable();

        var source = new SecretConfigurationSource(providerMock.Object);
        var provider = (SecretConfigurationProvider)source.Build(new ConfigurationBuilder());

        // Act
        provider.Load();

        // Assert
        providerMock.Verify(p => p.GetSecretsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Constructor_AllowsNullSecretProvider_BuildStillReturnsProvider()
    {
        // Arrange
        var source = new SecretConfigurationSource(secretProvider: null!);

        // Act
        var provider = source.Build(new ConfigurationBuilder());

        // Assert
        Assert.NotNull(provider);
        Assert.IsType<SecretConfigurationProvider>(provider);
    }
}