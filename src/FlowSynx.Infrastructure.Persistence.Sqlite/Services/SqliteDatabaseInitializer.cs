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
            await using var context = _contextFactory.CreateDbContext();
            var result = await context.Database.EnsureCreatedAsync(cancellationToken);

            if (result)
                _logger.LogInformation("Application database created successfully (SQLite).");
            else
                _logger.LogInformation("Application database already exists (SQLite).");
        }
        catch (Exception ex)
        {
            throw new FlowSynxException(
                (int)ErrorCode.DatabaseCreation,
                $"Error occurred while connecting the application database: {ex.Message}");
        }
    }
}