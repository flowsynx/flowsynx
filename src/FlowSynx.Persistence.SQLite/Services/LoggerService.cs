using Microsoft.EntityFrameworkCore;
using FlowSynx.Domain.Interfaces;
using FlowSynx.Domain.Entities.Logs;
using FlowSynx.Persistence.SQLite.Contexts;
using System.Linq.Expressions;

namespace FlowSynx.Persistence.SQLite.Services;

public class LoggerService : ILoggerService
{
    private readonly IDbContextFactory<LoggerContext> _logContextFactory;

    public LoggerService(IDbContextFactory<LoggerContext> logContextFactory)
    {
        _logContextFactory = logContextFactory;
    }

    public async Task<IReadOnlyCollection<Log>> All(Expression<Func<Log, bool>>? predicate, CancellationToken cancellationToken)
    {
        using var context = _logContextFactory.CreateDbContext();
        var logs = context.Logs;

        if (predicate == null)
            return await logs.ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        else
            return await logs.Where(predicate).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<Log?> Get(string userId, Guid id, CancellationToken cancellationToken)
    {
        using var context = _logContextFactory.CreateDbContext();
        return await context.Logs
            .FindAsync(new object?[] { userId, id }, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task Add(Log log, CancellationToken cancellationToken)
    {
        try
        {
            using var context = _logContextFactory.CreateDbContext();
            await context.Logs.AddAsync(log);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred saving changes.");
            Console.WriteLine(ex.InnerException?.Message);
        }
    }
}