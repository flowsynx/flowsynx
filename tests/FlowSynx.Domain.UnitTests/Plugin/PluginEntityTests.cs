using FlowSynx.Domain.Plugin;
using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.UnitTests.Plugin;

public class PluginEntityTests
{
    [Fact]
    public void PluginEntity_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var plugin = new PluginEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user123",
            Type = "Storage",
            Version = "1.0.0",
            PluginLocation = "/path/to/plugin"
        };

        // Assert
        Assert.Empty(plugin.Owners);
        Assert.Empty(plugin.Specifications);
        Assert.False(plugin.IsDeleted);
        Assert.Equal(default(DateTime), plugin.LastUpdated);
    }

    [Fact]
    public void PluginEntity_ShouldSetAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = "user123";
        var type = "Storage";
        var version = "1.0.0";
        var description = "Test plugin";
        var license = "MIT";
        var licenseUrl = "https://license.url";
        var icon = "icon.png";
        var projectUrl = "https://project.url";
        var repositoryUrl = "https://repo.url";
        var copyright = "Copyright 2025";
        var lastUpdated = DateTime.UtcNow;
        var owners = new List<string> { "owner1", "owner2" };
        var checksum = "abc123";
        var pluginLocation = "/path/to/plugin";
        var specifications = new List<PluginSpecification>
        {
            new() { Name = "Spec1", Type = "String", DefaultValue = "", IsRequired = false }
        };

        // Act
        var plugin = new PluginEntity
        {
            Id = id,
            UserId = userId,
            Type = type,
            Version = version,
            Description = description,
            License = license,
            LicenseUrl = licenseUrl,
            Icon = icon,
            ProjectUrl = projectUrl,
            RepositoryUrl = repositoryUrl,
            Copyright = copyright,
            LastUpdated = lastUpdated,
            Owners = owners,
            Checksum = checksum,
            PluginLocation = pluginLocation,
            Specifications = specifications,
            IsDeleted = true
        };

        // Assert
        Assert.Equal(id, plugin.Id);
        Assert.Equal(userId, plugin.UserId);
        Assert.Equal(type, plugin.Type);
        Assert.Equal(version, plugin.Version);
        Assert.Equal(description, plugin.Description);
        Assert.Equal(license, plugin.License);
        Assert.Equal(licenseUrl, plugin.LicenseUrl);
        Assert.Equal(icon, plugin.Icon);
        Assert.Equal(projectUrl, plugin.ProjectUrl);
        Assert.Equal(repositoryUrl, plugin.RepositoryUrl);
        Assert.Equal(copyright, plugin.Copyright);
        Assert.Equal(lastUpdated, plugin.LastUpdated);
        Assert.Equal(owners, plugin.Owners);
        Assert.Equal(checksum, plugin.Checksum);
        Assert.Equal(pluginLocation, plugin.PluginLocation);
        Assert.Single(plugin.Specifications);
        Assert.True(plugin.IsDeleted);
    }

    [Fact]
    public void PluginEntity_ShouldImplementISoftDeletable()
    {
        // Arrange & Act
        var plugin = new PluginEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user123",
            Type = "Storage",
            Version = "1.0.0",
            PluginLocation = "/path/to/plugin"
        };

        // Assert
        Assert.IsAssignableFrom<ISoftDeletable>(plugin);
    }

    [Fact]
    public void PluginEntity_ShouldInheritFromAuditableEntity()
    {
        // Arrange & Act
        var plugin = new PluginEntity
        {
            Id = Guid.NewGuid(),
            UserId = "user123",
            Type = "Storage",
            Version = "1.0.0",
            PluginLocation = "/path/to/plugin",
            CreatedBy = "admin",
            CreatedOn = DateTime.UtcNow
        };

        // Assert
        Assert.NotNull(plugin.CreatedBy);
        Assert.NotNull(plugin.CreatedOn);
    }
}