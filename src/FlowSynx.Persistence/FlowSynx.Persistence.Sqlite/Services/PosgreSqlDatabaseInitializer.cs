using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.Persistence.Sqlite.Contexts;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Persistence.Sqlite.Services;

public class PosgreSqlDatabaseInitializer : IDatabaseInitializer
{
    private readonly IDbContextFactory<ApplicationContext> _contextFactory;
    private readonly ILogger<PosgreSqlDatabaseInitializer> _logger;

    public PosgreSqlDatabaseInitializer(
        IDbContextFactory<ApplicationContext> contextFactory,
        ILogger<PosgreSqlDatabaseInitializer> logger)
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