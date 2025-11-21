using FlowSynx.Domain.Log;

namespace FlowSynx.Domain.UnitTests.Log;

public class LogEntityTests
{
    [Fact]
    public void LogEntity_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var logEntity = new LogEntity
        {
            Message = "Test message",
            Level = LogsLevel.Info,
            TimeStamp = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(Guid.Empty, logEntity.Id);
        Assert.Equal(string.Empty, logEntity.UserId);
        Assert.Equal(string.Empty, logEntity.Category);
        Assert.Null(logEntity.Exception);
        Assert.Null(logEntity.Scope);
    }

    [Fact]
    public void LogEntity_ShouldSetAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = "user123";
        var message = "Error occurred";
        var level = LogsLevel.Fail;
        var category = "Application";
        var timestamp = DateTime.UtcNow;
        var exception = "System.Exception: Test exception";
        var scope = "TestScope";

        // Act
        var logEntity = new LogEntity
        {
            Id = id,
            UserId = userId,
            Message = message,
            Level = level,
            Category = category,
            TimeStamp = timestamp,
            Exception = exception,
            Scope = scope
        };

        // Assert
        Assert.Equal(id, logEntity.Id);
        Assert.Equal(userId, logEntity.UserId);
        Assert.Equal(message, logEntity.Message);
        Assert.Equal(level, logEntity.Level);
        Assert.Equal(category, logEntity.Category);
        Assert.Equal(timestamp, logEntity.TimeStamp);
        Assert.Equal(exception, logEntity.Exception);
        Assert.Equal(scope, logEntity.Scope);
    }

    [Fact]
    public void LogEntity_WithDifferentLogLevels_ShouldWork()
    {
        // Arrange & Act
        var debugLog = new LogEntity { Message = "Debug", Level = LogsLevel.Dbug, TimeStamp = DateTime.UtcNow };
        var infoLog = new LogEntity { Message = "Info", Level = LogsLevel.Info, TimeStamp = DateTime.UtcNow };
        var warnLog = new LogEntity { Message = "Warn", Level = LogsLevel.Warn, TimeStamp = DateTime.UtcNow };
        var errorLog = new LogEntity { Message = "Error", Level = LogsLevel.Fail, TimeStamp = DateTime.UtcNow };
        var criticalLog = new LogEntity { Message = "Critical", Level = LogsLevel.Crit, TimeStamp = DateTime.UtcNow };

        // Assert
        Assert.Equal(LogsLevel.Dbug, debugLog.Level);
        Assert.Equal(LogsLevel.Info, infoLog.Level);
        Assert.Equal(LogsLevel.Warn, warnLog.Level);
        Assert.Equal(LogsLevel.Fail, errorLog.Level);
        Assert.Equal(LogsLevel.Crit, criticalLog.Level);
    }

    [Fact]
    public void LogEntity_UserId_CanBeNull()
    {
        // Arrange & Act
        var logEntity = new LogEntity
        {
            UserId = null,
            Message = "Test",
            Level = LogsLevel.Info,
            TimeStamp = DateTime.UtcNow
        };

        // Assert
        Assert.Null(logEntity.UserId);
    }
}