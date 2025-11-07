using FlowSynx.Domain.Log;
using FlowSynx.Persistence.Logging.Sqlite.Configurations;
using Microsoft.EntityFrameworkCore;

namespace FlowSynx.Persistence.Logging.Sqlite.UnitTests.Configurations;

public class LoggerConfigurationTests
{
    [Fact]
    public void Configure_ShouldSetPrimaryKey()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var config = new LoggerConfiguration();

        // Act
        config.Configure(modelBuilder.Entity<LogEntity>());
        var entity = modelBuilder.Model.FindEntityType(typeof(LogEntity));
        var key = entity.FindPrimaryKey();

        // Assert
        Assert.NotNull(key);
        Assert.Single(key.Properties);
        Assert.Equal("Id", key.Properties[0].Name);
    }

    [Fact]
    public void Configure_ShouldSetRequiredProperties()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var config = new LoggerConfiguration();

        // Act
        config.Configure(modelBuilder.Entity<LogEntity>());
        var entity = modelBuilder.Model.FindEntityType(typeof(LogEntity));

        // Assert
        Assert.False(entity.FindProperty(nameof(LogEntity.Id)).IsNullable);
        Assert.False(entity.FindProperty(nameof(LogEntity.Message)) is { IsNullable: true });
        Assert.False(entity.FindProperty(nameof(LogEntity.TimeStamp)) is { IsNullable: true });
        Assert.False(entity.FindProperty(nameof(LogEntity.Level)) is { IsNullable: true });
    }

    [Fact]
    public void Configure_ShouldSetEnumConverterForLevel()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var config = new LoggerConfiguration();

        // Act
        config.Configure(modelBuilder.Entity<LogEntity>());
        var entity = modelBuilder.Model.FindEntityType(typeof(LogEntity));
        var levelProperty = entity?.FindProperty(nameof(LogEntity.Level));
        var converter = levelProperty?.GetValueConverter();

        // Assert
        Assert.NotNull(converter);

        var enumValue = LogsLevel.Warn;
        var toProvider = converter.ConvertToProvider.Invoke(enumValue);
        var fromProvider = converter.ConvertFromProvider.Invoke(toProvider);

        Assert.Equal("Warn", toProvider);
        Assert.Equal(LogsLevel.Warn, fromProvider);
    }
}