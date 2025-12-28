using FlowSynx.Application.Services;
using FlowSynx.Persistence.Postgres.Contexts;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FlowSynx.Domain.Primitives;

namespace FlowSynx.Persistence.Postgres.Services;

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
            await context.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS citext;", cancellationToken);

            if (result)
                _logger.LogInformation("Application database created successfully.");
            else
                _logger.LogInformation("Application database already exists.");
        }
        catch (Exception ex)
        {
            throw new FlowSynxException(
                (int)ErrorCode.DatabaseCreation,
                $"Error occurred while connecting the application database: {ex.Message}");
        }
    }
}