using FlowSynx.Persistence.Sqlite.Contexts;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FlowSynx.Domain.Primitives;
using FlowSynx.Infrastructure.Persistence;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Services;

public class SqliteDatabaseInitializer : IDatabaseInitializer
{
    private readonly IDbContextFactory<SqliteApplicationContext> _contextFactory;
    private readonly ILogger<SqliteDatabaseInitializer> _logger;

    public SqliteDatabaseInitializer(
        IDbContextFactory<SqliteApplicationContext> contextFactory,
        ILogger<SqliteDatabaseInitializer> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task EnsureDatabaseCreatedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var result = await context.Database.EnsureCreatedAsync(cancellationToken);

            if (result)
                _logger.LogInformation("Application database created successfully (SQLite).");
            else
                _logger.LogInformation("Application database already exists (SQLite).");

            if (!await context.Tenants.AnyAsync(cancellationToken))
            {
                context.Tenants.Add(new Domain.Entities.Tenant
                {
                    Name = "FlowSynx Genome Platform",
                    Code = "FSX",
                    IsActive = true
                });
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Default tenant created successfully.");

            }
        }
        catch (Exception ex)
        {
            throw new FlowSynxException(
                (int)ErrorCode.DatabaseCreation,
                $"Error occurred while connecting the application database: {ex.Message}");
        }
    }
}