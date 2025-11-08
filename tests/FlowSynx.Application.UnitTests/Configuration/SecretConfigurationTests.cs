using FlowSynx.Application.Configuration.Secrets;
using FlowSynx.Application.Localizations;
using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlowSynx.Application.UnitTests.Configuration;

public class SecretConfigurationTests
{
    private readonly Mock<ILogger> _mockLogger = new();

    [Fact]
    public void ValidateSecretProviders_ShouldReturn_WhenDisabled()
    {
        // Arrange
        var config = new SecretConfiguration
        {
            Enabled = false
        };

        // Act (should simply return, no logging or exception)
        config.ValidateSecretProviders(_mockLogger.Object);

        // Assert
        _mockLogger.VerifyNoOtherCalls();
    }

    [Fact]
    public void ValidateSecretProviders_ShouldLogProviders_WhenEnabled()
    {
        // Arrange
        var config = new SecretConfiguration
        {
            Enabled = true,
            Providers = new Dictionary<string, SecretProviderConfiguration>
            {
                { "Infisical", new SecretProviderConfiguration() },
                { "Vault", new SecretProviderConfiguration() }
            },
            DefaultProvider = "Infisical"
        };

        // Act
        config.ValidateSecretProviders(_mockLogger.Object);

        // Assert: verify provider logs
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Secret provider 'Infisical' configured")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Secret provider 'Vault' configured")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Default secret provider name set to: Infisical")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ValidateSecretProviders_ShouldThrow_WhenDefaultProviderNotInList()
    {
        // Arrange
        Localization.Instance = new Mock<ILocalization>().Object; // Dummy instance

        var mockLocalization = new Mock<ILocalization>();
        mockLocalization
            .Setup(x => x.Get(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns((string key, object[] args) => $"{key}: {string.Join(", ", args)}");

        Localization.Instance = mockLocalization.Object;

        var config = new SecretConfiguration
        {
            Enabled = true,
            Providers = new Dictionary<string, SecretProviderConfiguration>
        {
            { "Vault", new SecretProviderConfiguration() }
        },
            DefaultProvider = "Infisical"
        };

        // Act & Assert
        var ex = Assert.Throws<FlowSynxException>(() =>
            config.ValidateSecretProviders(Mock.Of<ILogger>()));

        Assert.Equal((int)ErrorCode.SecretConfigurationInvalidProviderName, ex.ErrorCode);
        Assert.Contains("Infisical", ex.Message);
        Assert.Contains("Vault", ex.Message);
    }


    [Fact]
    public void ValidateSecretProviders_ShouldLogWarning_WhenDefaultProviderIsNullOrEmpty()
    {
        // Arrange
        var config = new SecretConfiguration
        {
            Enabled = true,
            Providers = new Dictionary<string, SecretProviderConfiguration>
            {
                { "Vault", new SecretProviderConfiguration() }
            },
            DefaultProvider = null
        };

        // Act
        config.ValidateSecretProviders(_mockLogger.Object);

        // Assert: should log warning
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No default secret provider name is defined.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}