using FlowSynx.Application.Abstractions.Services;
using FlowSynx.Domain.Primitives;
using FlowSynx.Infrastructure.Persistence.Abstractions;
using FlowSynx.Persistence.Sqlite.EntityConfigurations;
using FlowSynx.PluginCore.Exceptions;
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
        ISystemClock systemClock)
        : base(contextOptions, logger, httpContextAccessor, systemClock)
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
            var errorMessage = new ErrorMessage((int)ErrorCode.DatabaseModelCreating, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    private void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AuditTrailConfiguration());
        modelBuilder.ApplyConfiguration(new GeneBlueprintConfiguration());
        modelBuilder.ApplyConfiguration(new GeneInstanceConfiguration());
        modelBuilder.ApplyConfiguration(new ChromosomeConfiguration());
        modelBuilder.ApplyConfiguration(new GenomeConfiguration());
        modelBuilder.ApplyConfiguration(new TenantEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TenantContactEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TenantSecretEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TenantSecretConfigEntityConfiguration());
    }
}