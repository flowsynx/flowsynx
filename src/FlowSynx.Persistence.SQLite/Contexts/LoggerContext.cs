using Microsoft.EntityFrameworkCore;
using FlowSynx.Persistence.SQLite.Configurations;
using FlowSynx.Domain.Log;

namespace FlowSynx.Persistence.SQLite.Contexts;

public class LoggerContext(DbContextOptions<LoggerContext> contextOptions) : DbContext(contextOptions)
{
    public DbSet<LogEntity> Logs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfiguration(new LoggerConfiguration());
    }
}