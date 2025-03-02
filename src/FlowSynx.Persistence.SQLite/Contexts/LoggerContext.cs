using Microsoft.EntityFrameworkCore;
using FlowSynx.Domain.Entities.Logs;
using FlowSynx.Persistence.SQLite.Configurations;

namespace FlowSynx.Persistence.SQLite.Contexts;

public class LoggerContext : DbContext
{
    public LoggerContext(DbContextOptions<LoggerContext> contextOptions)
        : base(contextOptions)
    {
    }

    public DbSet<Log> Logs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfiguration(new LoggerConfiguration());
    }
}