using Microsoft.EntityFrameworkCore;
using FlowSynx.Domain.Interfaces;
using FlowSynx.Persistence.Postgres.Contexts;

namespace FlowSynx.Persistence.Postgres.Services;

public class TransactionService : ITransactionService
{
    private readonly IDbContextFactory<ApplicationContext> _appContextFactory;

    public TransactionService(IDbContextFactory<ApplicationContext> dbContextFactory)
    {
        _appContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }

    public async Task TransactionAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            await action();
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            transaction?.Dispose();
            context?.Dispose();
        }
    }
}