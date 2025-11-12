using FlowSynx.Application.Configuration.Core.Secrets;

namespace FlowSynx.Application.UnitTests.Configuration;

public class SecretProviderConfigurationTests
{
    [Fact]
    public void Constructor_ShouldUse_OrdinalIgnoreCaseComparer()
    {
        // Act
        var config = new SecretProviderConfiguration();

        // Assert
        Assert.Same(StringComparer.OrdinalIgnoreCase, config.Comparer);
    }

    [Fact]
    public void Indexer_ShouldBeCaseInsensitive()
    {
        // Arrange
        var config = new SecretProviderConfiguration();

        // Act
        config["ApiKey"] = "value1";
        var existsLower = config.ContainsKey("apikey");
        var valueUpper = config["APIKEY"];

        // Assert
        Assert.True(existsLower);
        Assert.Equal("value1", valueUpper);
    }

    [Fact]
    public void Add_SameKeyDifferentCase_ShouldThrow()
    {
        // Arrange
        var config = new SecretProviderConfiguration();
        config.Add("Token", "v1");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => config.Add("token", "v2"));
        Assert.Equal(1, config.Count);
        Assert.Equal("v1", config["TOKEN"]);
    }

    [Fact]
    public void Remove_ShouldBeCaseInsensitive()
    {
        // Arrange
        var config = new SecretProviderConfiguration
        {
            ["UserName"] = "admin"
        };

        // Act
        var removed = config.Remove("username");

        // Assert
        Assert.True(removed);
        Assert.False(config.ContainsKey("USERNAME"));
        Assert.Equal(0, config.Count);
    }
}