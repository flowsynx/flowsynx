using System;
using System.Collections.Generic;
using FlowSynx.Application.Configuration;
using FlowSynx.Application.Secrets;
using FlowSynx.Infrastructure.Secrets;
using Moq;
using Xunit;

namespace FlowSynx.Infrastructure.UnitTests.Secrets;

public class SecretFactoryTests
{
    [Fact]
    public void GetDefaultProvider_ReturnsNull_WhenSecretsAreDisabled()
    {
        // Arrange
        var config = new SecretConfiguration
        {
            Enabled = false,
            DefaultProvider = "Infisical",
            Providers = new Dictionary<string, SecretProviderConfiguration>
            {
                ["Infisical"] = new SecretProviderConfiguration()
            }
        };

        var providers = Array.Empty<ISecretProvider>();
        var factory = new SecretFactory(config, providers);

        // Act
        var result = factory.GetDefaultProvider();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetDefaultProvider_Throws_WhenNoConfigurationForDefaultProvider()
    {
        // Arrange
        var config = new SecretConfiguration
        {
            Enabled = true,
            DefaultProvider = "MissingProvider",
            Providers = new Dictionary<string, SecretProviderConfiguration>()
        };

        var providers = Array.Empty<ISecretProvider>();
        var factory = new SecretFactory(config, providers);

        // Act / Assert
        var ex = Assert.Throws<InvalidOperationException>(() => factory.GetDefaultProvider());
        Assert.Contains("No secret configuration found", ex.Message);
    }

    [Fact]
    public void GetDefaultProvider_Throws_WhenNoProviderImplementationFound()
    {
        // Arrange
        var config = new SecretConfiguration
        {
            Enabled = true,
            DefaultProvider = "Infisical",
            Providers = new Dictionary<string, SecretProviderConfiguration>
            {
                ["Infisical"] = new SecretProviderConfiguration()
            }
        };

        var providers = Array.Empty<ISecretProvider>();
        var factory = new SecretFactory(config, providers);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => factory.GetDefaultProvider());
    }

    [Fact]
    public void GetDefaultProvider_ReturnsProvider_WhenImplementationFound()
    {
        // Arrange
        var config = new SecretConfiguration
        {
            Enabled = true,
            DefaultProvider = "Infisical",
            Providers = new Dictionary<string, SecretProviderConfiguration>
            {
                ["Infisical"] = new SecretProviderConfiguration { ["k"] = "v" }
            }
        };

        var providerMock = new Mock<ISecretProvider>(MockBehavior.Strict);
        providerMock.SetupGet(p => p.Name).Returns("Infisical");

        var providers = new[] { providerMock.Object };
        var factory = new SecretFactory(config, providers);

        // Act
        var result = factory.GetDefaultProvider();

        // Assert
        Assert.Same(providerMock.Object, result);
    }

    [Fact]
    public void GetDefaultProvider_PerformsCaseInsensitiveNameMatch()
    {
        // Arrange
        var config = new SecretConfiguration
        {
            Enabled = true,
            DefaultProvider = "InFiSiCaL",
            Providers = new Dictionary<string, SecretProviderConfiguration>
            {
                ["InFiSiCaL"] = new SecretProviderConfiguration()
            }
        };

        var providerMock = new Mock<ISecretProvider>(MockBehavior.Strict);
        providerMock.SetupGet(p => p.Name).Returns("infisical");

        var providers = new[] { providerMock.Object };
        var factory = new SecretFactory(config, providers);

        // Act
        var result = factory.GetDefaultProvider();

        // Assert
        Assert.Same(providerMock.Object, result);
    }

    [Fact]
    public void GetDefaultProvider_CallsConfigure_WhenProviderIsConfigurable()
    {
        // Arrange
        var defaultName = "Infisical";
        var providerConfig = new SecretProviderConfiguration
        {
            ["HostUri"] = "https://example.com",
            ["ProjectId"] = "proj",
        };

        var config = new SecretConfiguration
        {
            Enabled = true,
            DefaultProvider = defaultName,
            Providers = new Dictionary<string, SecretProviderConfiguration>
            {
                [defaultName] = providerConfig
            }
        };

        var providerMock = new Mock<ISecretProvider>(MockBehavior.Strict);
        providerMock.SetupGet(p => p.Name).Returns(defaultName);

        var configurable = providerMock.As<IConfigurableSecret>();
        configurable
            .Setup(p => p.Configure(It.IsAny<Dictionary<string, string>>()))
            .Verifiable();

        var providers = new[] { providerMock.Object };
        var factory = new SecretFactory(config, providers);

        // Act
        var result = factory.GetDefaultProvider();

        // Assert
        Assert.Same(providerMock.Object, result);
        configurable.Verify(p => p.Configure(It.Is<Dictionary<string, string>>(d =>
            d.ContainsKey("HostUri") && d["HostUri"] == "https://example.com" &&
            d.ContainsKey("ProjectId") && d["ProjectId"] == "proj")), Times.Once);
    }
}