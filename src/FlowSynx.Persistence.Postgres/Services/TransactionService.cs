using Microsoft.EntityFrameworkCore;
using FlowSynx.Persistence.Postgres.Contexts;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Domain;

namespace FlowSynx.Persistence.Postgres.Services;

public class TransactionService : ITransactionService
{
    private readonly IDbContextFactory<ApplicationContext> _appContextFactory;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(IDbContextFactory<ApplicationContext> appContextFactory,
        ILogger<TransactionService> logger)
    {
        ArgumentNullException.ThrowIfNull(appContextFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
        _logger = logger;
    }

    public async Task TransactionAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await action();
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            var errorMessage = new ErrorMessage((int)ErrorCode.DatabaseTransaction, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }
}