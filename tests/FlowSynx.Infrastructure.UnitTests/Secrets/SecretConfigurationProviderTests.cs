using FlowSynx.Application.Secrets;
using FlowSynx.Infrastructure.Secrets;
using Moq;

namespace FlowSynx.Infrastructure.UnitTests.Secrets;

public class SecretConfigurationProviderTests
{
    [Fact]
    public void Constructor_DoesNotThrow_WithValidProvider()
    {
        // Arrange
        var providerMock = new Mock<ISecretProvider>(MockBehavior.Strict);

        // Act
        var provider = new SecretConfigurationProvider(providerMock.Object);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void Constructor_AllowsNullSecretProvider_InstanceCreated()
    {
        // Act
        var provider = new SecretConfigurationProvider(secretProvider: null!);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void Load_WhenProviderIsNull_Throws()
    {
        // Arrange
        var provider = new SecretConfigurationProvider(secretProvider: null!);

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => provider.Load());
    }

    [Fact]
    public void Load_WhenSecretsAreEmpty_DataIsEmpty()
    {
        // Arrange
        var secrets = Array.Empty<KeyValuePair<string, string>>();
        var providerMock = new Mock<ISecretProvider>(MockBehavior.Strict);
        providerMock
            .Setup(p => p.GetSecretsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(secrets)
            .Verifiable();

        var provider = new SecretConfigurationProvider(providerMock.Object);

        // Act
        provider.Load();

        // Assert
        providerMock.Verify(p => p.GetSecretsAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Empty(provider.GetChildKeys(Enumerable.Empty<string>(), parentPath: null));
        Assert.False(provider.TryGet("any", out _));
    }

    [Fact]
    public void Load_PopulatesData_WithReturnedSecrets()
    {
        // Arrange
        var secrets = new List<KeyValuePair<string, string>>
        {
            new("Database:ConnectionString", "Host=localhost;"),
            new("Api:Key", "secret-value"),
        };

        var providerMock = new Mock<ISecretProvider>(MockBehavior.Strict);
        providerMock
            .Setup(p => p.GetSecretsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(secrets)
            .Verifiable();

        var provider = new SecretConfigurationProvider(providerMock.Object);

        // Act
        provider.Load();

        // Assert
        providerMock.Verify(p => p.GetSecretsAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.True(provider.TryGet("Database:ConnectionString", out var connStr));
        Assert.Equal("Host=localhost;", connStr);

        Assert.True(provider.TryGet("Api:Key", out var apiKey));
        Assert.Equal("secret-value", apiKey);
    }

    [Fact]
    public void Load_KeysAreCaseInsensitive()
    {
        // Arrange
        var secrets = new List<KeyValuePair<string, string>>
        {
            new("ConnStr", "value1"),
        };

        var providerMock = new Mock<ISecretProvider>(MockBehavior.Strict);
        providerMock
            .Setup(p => p.GetSecretsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(secrets)
            .Verifiable();

        var provider = new SecretConfigurationProvider(providerMock.Object);

        // Act
        provider.Load();

        // Assert
        providerMock.Verify(p => p.GetSecretsAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.True(provider.TryGet("connstr", out var lower));
        Assert.Equal("value1", lower);
        Assert.True(provider.TryGet("CONNSTR", out var upper));
        Assert.Equal("value1", upper);
    }

    [Fact]
    public void Load_WhenProviderThrows_PropagatesException()
    {
        // Arrange
        var providerMock = new Mock<ISecretProvider>(MockBehavior.Strict);
        providerMock
            .Setup(p => p.GetSecretsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var provider = new SecretConfigurationProvider(providerMock.Object);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => provider.Load());
        Assert.Equal("boom", ex.Message);
    }
}