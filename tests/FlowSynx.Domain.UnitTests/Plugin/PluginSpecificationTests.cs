using FlowSynx.Domain.Plugin;

namespace FlowSynx.Domain.UnitTests.Plugin;

public class PluginSpecificationTests
{
    [Fact]
    public void PluginSpecification_ShouldSetRequiredProperties()
    {
        // Arrange & Act
        var spec = new PluginSpecification
        {
            Name = "TestSpec",
            Type = "String"
        };

        // Assert
        Assert.Equal("TestSpec", spec.Name);
        Assert.Equal("String", spec.Type);
        Assert.False(spec.IsRequired);
    }

    [Fact]
    public void PluginSpecification_ShouldSetAllProperties()
    {
        // Arrange & Act
        var spec = new PluginSpecification
        {
            Name = "ConnectionString",
            Type = "String",
            DefaultValue = "DefaultValue",
            Description = "Database connection string",
            IsRequired = true
        };

        // Assert
        Assert.Equal("ConnectionString", spec.Name);
        Assert.Equal("String", spec.Type);
        Assert.Equal("DefaultValue", spec.DefaultValue);
        Assert.Equal("Database connection string", spec.Description);
        Assert.True(spec.IsRequired);
    }

    [Fact]
    public void PluginSpecification_IsRequired_DefaultShouldBeFalse()
    {
        // Arrange & Act
        var spec = new PluginSpecification
        {
            Name = "OptionalField",
            Type = "Integer"
        };

        // Assert
        Assert.False(spec.IsRequired);
    }

    [Fact]
    public void PluginSpecification_BooleanProperties_ShouldBeIndependent()
    {
        // Arrange & Act
        var spec1 = new PluginSpecification { Name = "Spec1", Type = "Type1" };
        var spec2 = new PluginSpecification { Name = "Spec2", Type = "Type2" };
        var spec3 = new PluginSpecification { Name = "Spec3", Type = "Type3", IsRequired = true };

        // Assert
        Assert.False(spec1.IsRequired);
        Assert.False(spec2.IsRequired);
        Assert.True(spec3.IsRequired);
    }
}