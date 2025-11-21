using FlowSynx.Domain.PluginConfig;

namespace FlowSynx.Domain.UnitTests.PluginConfig;

public class PluginConfigurationEntityTests
{
    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var entity = new PluginConfigurationEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user123",
            Name = "TestConfig",
            Type = "Storage",
            Version = "1.0.0"
        };

        // Act
        var result = entity.ToString();

        // Assert
        Assert.Equal("user123@Storage:TestConfig", result);
    }

    [Fact]
    public void Equals_WithSameUserIdAndName_ShouldReturnTrue()
    {
        // Arrange
        var entity1 = new PluginConfigurationEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user123",
            Name = "Config1",
            Type = "Storage",
            Version = "1.0.0"
        };

        var entity2 = new PluginConfigurationEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user123",
            Name = "Config1",
            Type = "Processor",
            Version = "2.0.0"
        };

        // Act
        var result = entity1.Equals(entity2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_WithDifferentUserId_ShouldReturnFalse()
    {
        // Arrange
        var entity1 = new PluginConfigurationEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user123",
            Name = "Config1",
            Type = "Storage",
            Version = "1.0.0"
        };

        var entity2 = new PluginConfigurationEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user456",
            Name = "Config1",
            Type = "Storage",
            Version = "1.0.0"
        };

        // Act
        var result = entity1.Equals(entity2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_WithDifferentName_ShouldReturnFalse()
    {
        // Arrange
        var entity1 = new PluginConfigurationEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user123",
            Name = "Config1",
            Type = "Storage",
            Version = "1.0.0"
        };

        var entity2 = new PluginConfigurationEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user123",
            Name = "Config2",
            Type = "Storage",
            Version = "1.0.0"
        };

        // Act
        var result = entity1.Equals(entity2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var entity = new PluginConfigurationEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user123",
            Name = "Config1",
            Type = "Storage",
            Version = "1.0.0"
        };

        // Act
        var result = entity.Equals(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_WithSameReference_ShouldReturnTrue()
    {
        // Arrange
        var entity = new PluginConfigurationEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user123",
            Name = "Config1",
            Type = "Storage",
            Version = "1.0.0"
        };

        // Act
        var result = entity.Equals(entity);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_WithObjectType_ShouldWorkCorrectly()
    {
        // Arrange
        var entity1 = new PluginConfigurationEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user123",
            Name = "Config1",
            Type = "Storage",
            Version = "1.0.0"
        };

        object entity2 = new PluginConfigurationEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user123",
            Name = "Config1",
            Type = "Storage",
            Version = "1.0.0"
        };

        // Act
        var result = entity1.Equals(entity2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetHashCode_WithSameUserIdAndName_ShouldReturnSameHashCode()
    {
        // Arrange
        var entity1 = new PluginConfigurationEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user123",
            Name = "Config1",
            Type = "Storage",
            Version = "1.0.0"
        };

        var entity2 = new PluginConfigurationEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user123",
            Name = "Config1",
            Type = "Processor",
            Version = "2.0.0"
        };

        // Act
        var hash1 = entity1.GetHashCode();
        var hash2 = entity2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_WithDifferentUserIdOrName_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var entity1 = new PluginConfigurationEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user123",
            Name = "Config1",
            Type = "Storage",
            Version = "1.0.0"
        };

        var entity2 = new PluginConfigurationEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user456",
            Name = "Config1",
            Type = "Storage",
            Version = "1.0.0"
        };

        // Act
        var hash1 = entity1.GetHashCode();
        var hash2 = entity2.GetHashCode();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void IsDeleted_DefaultValue_ShouldBeFalse()
    {
        // Arrange & Act
        var entity = new PluginConfigurationEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user123",
            Name = "Config1",
            Type = "Storage",
            Version = "1.0.0"
        };

        // Assert
        Assert.False(entity.IsDeleted);
    }

    [Fact]
    public void Specifications_ShouldBeSettable()
    {
        // Arrange
        var entity = new PluginConfigurationEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user123",
            Name = "Config1",
            Type = "Storage",
            Version = "1.0.0"
        };

        var specifications = new PluginConfigurationSpecifications();

        // Act
        entity.Specifications = specifications;

        // Assert
        Assert.NotNull(entity.Specifications);
        Assert.Same(specifications, entity.Specifications);
    }

    [Fact]
    public void Entity_ShouldInheritFromAuditableEntity()
    {
        // Arrange & Act
        var entity = new PluginConfigurationEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user123",
            Name = "Config1",
            Type = "Storage",
            Version = "1.0.0",
            CreatedBy = "admin",
            CreatedOn = DateTime.UtcNow,
            LastModifiedBy = "admin",
            LastModifiedOn = DateTime.UtcNow
        };

        // Assert
        Assert.NotNull(entity.CreatedBy);
        Assert.NotNull(entity.CreatedOn);
        Assert.NotNull(entity.LastModifiedBy);
        Assert.NotNull(entity.LastModifiedOn);
    }
}