using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FlowSynx.Application.Secrets;
using FlowSynx.Infrastructure.Extensions;
using FlowSynx.Infrastructure.Secrets;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace FlowSynx.Infrastructure.UnitTests.Extensions;

public class ConfigurationBuilderExtensionsTests
{
    [Fact]
    public void AddSecrets_ReturnsSameBuilder_WhenProviderIsNull()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var initialCount = builder.Sources.Count;

        // Act
        var result = builder.AddSecrets(provider: null);

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(initialCount, builder.Sources.Count);
    }

    [Fact]
    public void AddSecrets_AddsSecretConfigurationSource_WhenProviderIsProvided()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var providerMock = new Mock<ISecretProvider>(MockBehavior.Strict);

        // Act
        var result = builder.AddSecrets(providerMock.Object);

        // Assert
        Assert.Same(builder, result);
        Assert.Contains(builder.Sources, s => s is SecretConfigurationSource);
    }

    [Fact]
    public void AddSecrets_BuildInvokesSecretProvider_WhenProviderIsProvided()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var secrets = new List<KeyValuePair<string, string>>();

        var providerMock = new Mock<ISecretProvider>(MockBehavior.Strict);
        providerMock
            .Setup(p => p.GetSecretsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(secrets)
            .Verifiable();

        // Act
        builder.AddSecrets(providerMock.Object);
        _ = builder.Build();

        // Assert
        providerMock.Verify(p => p.GetSecretsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void AddSecrets_ReturnsNull_WhenBuilderIsNull_AndProviderIsNull()
    {
        // Act
        var result = ConfigurationBuilderExtensions.AddSecrets(builder: null!, provider: null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void AddSecrets_ThrowsNullReference_WhenBuilderIsNull_AndProviderIsProvided()
    {
        // Arrange
        var providerMock = new Mock<ISecretProvider>(MockBehavior.Strict);

        // Act & Assert
        Assert.Throws<NullReferenceException>(() =>
            ConfigurationBuilderExtensions.AddSecrets(builder: null!, provider: providerMock.Object));
    }
}
