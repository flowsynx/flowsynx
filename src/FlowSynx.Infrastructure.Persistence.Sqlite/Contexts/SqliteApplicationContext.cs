using FlowSynx.BuildingBlocks.Clock;
using FlowSynx.Infrastructure.Persistence.Abstractions;
using FlowSynx.Infrastructure.Persistence.Abstractions.Exceptions;
using FlowSynx.Persistence.Sqlite.EntityConfigurations;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Persistence.Sqlite.Contexts;

public class SqliteApplicationContext : BaseDbContext
{
    private readonly ILogger<SqliteApplicationContext> _logger;

    public SqliteApplicationContext(
        DbContextOptions<SqliteApplicationContext> contextOptions,
        ILogger<SqliteApplicationContext> logger,
        IHttpContextAccessor httpContextAccessor,
        IClock clock)
        : base(contextOptions, logger, httpContextAccessor, clock)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        try
        {
            base.OnModelCreating(modelBuilder);
            ApplyEntityConfigurations(modelBuilder);
        }
        catch (Exception ex)
        {
            throw new DatabaseModelCreatingException(ex);
        }
    }

    private void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AuditTrailConfiguration());
        modelBuilder.ApplyConfiguration(new GeneConfiguration());
        modelBuilder.ApplyConfiguration(new GeneInstanceConfiguration());
        modelBuilder.ApplyConfiguration(new ChromosomeConfiguration());
        modelBuilder.ApplyConfiguration(new GenomeConfiguration());
        modelBuilder.ApplyConfiguration(new TenantEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TenantContactEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TenantSecretEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TenantSecretConfigEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ExecutionRecordConfiguration());
        modelBuilder.ApplyConfiguration(new ExecutionLogConfiguration());
        modelBuilder.ApplyConfiguration(new ExecutionArtifactConfiguration());
    }
}