using FlowSynx.Domain.Log;
using FlowSynx.Persistence.Logging.Sqlite.Contexts;
using FlowSynx.Persistence.Logging.Sqlite.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlowSynx.Persistence.Logging.Sqlite.UnitTests.Services;

public class LoggerServiceTests
{
    private static DbContextOptions<LoggerContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<LoggerContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private static LoggerService CreateLoggerService(DbContextOptions<LoggerContext> options)
    {
        var factoryMock = new Mock<IDbContextFactory<LoggerContext>>();
        factoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new LoggerContext(options)); // Return NEW context each time

        return new LoggerService(factoryMock.Object);
    }

    [Fact]
    public async Task Add_Should_Add_LogEntity()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var loggerService = CreateLoggerService(options);
        var log = new LogEntity
        {
            Id = Guid.NewGuid(),
            UserId = "User1",
            TimeStamp = DateTime.UtcNow,
            Message = "Test1",
            Level = LogLevel.Information.ToString()
        };

        // Act
        await loggerService.Add(log, CancellationToken.None);

        // Assert using fresh context
        await using var verifyContext = new LoggerContext(options);
        var result = await verifyContext.Logs.FirstOrDefaultAsync();
        Assert.NotNull(result);
        Assert.Equal("User1", result!.UserId);
    }

    [Fact]
    public async Task All_Should_Return_Logs_Without_Predicate()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        await using (var seedContext = new LoggerContext(options))
        {
            seedContext.Logs.Add(new LogEntity { 
                Id = Guid.NewGuid(), 
                UserId = "user1", 
                TimeStamp = DateTime.UtcNow.AddMinutes(-1),
                Message = "Test1",
                Level = LogLevel.Information.ToString()
            });
            seedContext.Logs.Add(new LogEntity { 
                Id = Guid.NewGuid(), 
                UserId = "user2", 
                TimeStamp = DateTime.UtcNow,
                Message = "Test2",
                Level = LogLevel.Information.ToString()
            });
            await seedContext.SaveChangesAsync();
        }

        var loggerService = CreateLoggerService(options);

        // Act
        var logs = await loggerService.All(null, CancellationToken.None);

        // Assert
        Assert.Equal(2, logs.Count);
    }

    [Fact]
    public async Task All_Should_Filter_With_Predicate()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        await using (var seedContext = new LoggerContext(options))
        {
            seedContext.Logs.Add(new LogEntity { 
                Id = Guid.NewGuid(), 
                UserId = "filter-me", 
                TimeStamp = DateTime.UtcNow,
                Message = "filter-me message",
                Level = LogLevel.Information.ToString()
            });
            seedContext.Logs.Add(new LogEntity { 
                Id = Guid.NewGuid(), 
                UserId = "skip-me", 
                TimeStamp = DateTime.UtcNow,
                Message = "skip-me message",
                Level = LogLevel.Information.ToString()
            });
            await seedContext.SaveChangesAsync();
        }

        var loggerService = CreateLoggerService(options);

        // Act
        var logs = await loggerService.All(log => log.UserId == "filter-me", CancellationToken.None);

        // Assert
        Assert.Single(logs);
        Assert.Equal("filter-me", logs.First().UserId);
    }

    [Fact]
    public async Task Get_Should_Return_LogEntity_By_Id()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = "my-user";
        var options = CreateInMemoryOptions();

        await using (var seedContext = new LoggerContext(options))
        {
            seedContext.Logs.Add(new LogEntity { 
                Id = id, 
                UserId = userId, 
                TimeStamp = DateTime.UtcNow,
                Message = "my-user message",
                Level = LogLevel.Information.ToString()
            });
            await seedContext.SaveChangesAsync();
        }

        var loggerService = CreateLoggerService(options);

        // Act
        var log = await loggerService.Get(userId, id, CancellationToken.None);

        // Assert
        Assert.NotNull(log);
        Assert.Equal(userId, log!.UserId);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_Return_True_If_CanConnect()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var loggerService = CreateLoggerService(options);

        // Act
        var result = await loggerService.CheckHealthAsync();

        // Assert
        Assert.True(result);
    }
}