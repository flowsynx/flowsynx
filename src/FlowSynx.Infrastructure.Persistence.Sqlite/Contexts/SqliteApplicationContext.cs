using FlowSynx.BuildingBlocks.Clock;
using FlowSynx.Infrastructure.Persistence.Abstractions;
using FlowSynx.Infrastructure.Persistence.Abstractions.Exceptions;
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SqliteApplicationContext).Assembly);
    }
}