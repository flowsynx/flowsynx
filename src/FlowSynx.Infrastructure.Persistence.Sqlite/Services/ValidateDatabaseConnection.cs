using FlowSynx.Application.Abstractions.Services;
using FlowSynx.Infrastructure.Persistence.Sqlite.Repositories;
using FlowSynx.Persistence.Sqlite.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Services;

public class ValidateDatabaseConnection: IValidateDatabaseConnection
{
    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;
    private readonly ILogger<TenantRepository> _logger;
    public ValidateDatabaseConnection(
        IDbContextFactory<SqliteApplicationContext> appContextFactory,
        ILogger<TenantRepository> logger)
    {
        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> ValidateConnection(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            await context.Database.CanConnectAsync(cancellationToken);
            _logger.LogInformation("Successfully connected to the SQLite database.");
            return true;
        }
        catch
        {
            return false;
        }
    }
}
