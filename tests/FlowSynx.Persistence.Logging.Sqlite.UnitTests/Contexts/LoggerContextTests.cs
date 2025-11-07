using FlowSynx.Domain.Log;
using FlowSynx.Persistence.Logging.Sqlite.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FlowSynx.Persistence.Logging.Sqlite.UnitTests.Contexts;

public class LoggerContextTests
{
    private DbContextOptions<LoggerContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<LoggerContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Unique DB per test
            .Options;
    }

    [Fact]
    public void CanInstantiateLoggerContext()
    {
        var options = CreateOptions();
        using var context = new LoggerContext(options);
        Assert.NotNull(context);
    }

    [Fact]
    public void CanAddAndRetrieveLog()
    {
        var options = CreateOptions();
        using (var context = new LoggerContext(options))
        {
            var log = new LogEntity { Id = Guid.NewGuid(), Level = LogsLevel.Info, Message = "Test", TimeStamp = DateTime.Now};
            context.Logs.Add(log);
            context.SaveChanges();
        }

        using (var context = new LoggerContext(options))
        {
            var log = context.Logs.FirstOrDefault();
            Assert.NotNull(log);
            Assert.Equal("Test", log.Message);
        }
    }

    [Fact]
    public void OnModelCreating_ConfiguresEntity()
    {
        var options = CreateOptions();
        using var context = new LoggerContext(options);
        var entityType = context.Model.FindEntityType(typeof(LogEntity));
        Assert.NotNull(entityType);

        var levelProperty = entityType.FindProperty(nameof(LogEntity.Level));
        Assert.NotNull(levelProperty);
    }
}