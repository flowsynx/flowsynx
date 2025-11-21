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
        Assert.Null(spec.DeclaringType);
        Assert.False(spec.IsReadable);
        Assert.False(spec.IsWritable);
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
            DeclaringType = "ConfigType",
            IsReadable = true,
            IsWritable = true,
            IsRequired = true
        };

        // Assert
        Assert.Equal("ConnectionString", spec.Name);
        Assert.Equal("String", spec.Type);
        Assert.Equal("ConfigType", spec.DeclaringType);
        Assert.True(spec.IsReadable);
        Assert.True(spec.IsWritable);
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
        var spec1 = new PluginSpecification { Name = "Spec1", Type = "Type1", IsReadable = true };
        var spec2 = new PluginSpecification { Name = "Spec2", Type = "Type2", IsWritable = true };
        var spec3 = new PluginSpecification { Name = "Spec3", Type = "Type3", IsRequired = true };

        // Assert
        Assert.True(spec1.IsReadable);
        Assert.False(spec1.IsWritable);
        Assert.False(spec1.IsRequired);

        Assert.False(spec2.IsReadable);
        Assert.True(spec2.IsWritable);
        Assert.False(spec2.IsRequired);

        Assert.False(spec3.IsReadable);
        Assert.False(spec3.IsWritable);
        Assert.True(spec3.IsRequired);
    }
}