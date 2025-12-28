using Microsoft.EntityFrameworkCore;
using FlowSynx.Persistence.Logging.Sqlite.Configurations;
using FlowSynx.Domain.Entities;

namespace FlowSynx.Persistence.Logging.Sqlite.Contexts;

public class LoggerContext(DbContextOptions<LoggerContext> contextOptions) : DbContext(contextOptions)
{
    public DbSet<LogEntity> Logs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new LoggerConfiguration());
    }
}