using FlowSynx.Application.Extensions;
using FlowSynx.Domain.PluginConfig;

namespace FlowSynx.Application.UnitTests.Extensions;

public class PluginConfigurationSpecificationsExtensionsTests
{
    [Fact]
    public void ToPluginConfigurationSpecifications_ShouldReturnEmptyInstance_WhenDictionaryIsNull()
    {
        // Arrange
        Dictionary<string, object?>? dictionary = null;

        // Act
        var result = dictionary.ToPluginConfigurationSpecifications();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<PluginConfigurationSpecifications>(result);
    }

    [Fact]
    public void ToPluginConfigurationSpecifications_ShouldReturnInstanceWithValues_WhenDictionaryIsNotNull()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?>
        {
            { "Key1", "Value1" },
            { "Key2", 42 },
            { "Key3", null }
        };

        // Act
        var result = dictionary.ToPluginConfigurationSpecifications();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<PluginConfigurationSpecifications>(result);
    }
}