using FlowSynx.Application;
using FlowSynx.Domain.Entities;
using FlowSynx.Domain.Primitives;
using FlowSynx.Persistence.Sqlite.Contexts;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Repositories;

public class LogEntryRepository : ILogEntryRepository
{
    private readonly IDbContextFactory<SqliteApplicationContext> _logContextFactory;

    public LogEntryRepository(IDbContextFactory<SqliteApplicationContext> logContextFactory)
    {
        _logContextFactory = logContextFactory ?? throw new ArgumentNullException(nameof(logContextFactory));
    }

    public async Task<IReadOnlyCollection<LogEntry>> All(
        Expression<Func<LogEntry, bool>>? predicate, 
        CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _logContextFactory.CreateDbContextAsync(cancellationToken);
            var logEntries = context.LogEntries;

            if (predicate == null)
            {
                return await logEntries
                    .OrderByDescending(x => x.TimeStamp)
                    .Take(512)
                    .ToListAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                return await logEntries
                    .Where(predicate)
                    .OrderByDescending(x => x.TimeStamp)
                    .Take(512)
                    .ToListAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.LogsList, ex.Message);
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<LogEntry?> Get(
        string userId, 
        Guid id, 
        CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _logContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.LogEntries
                .FirstOrDefaultAsync(l=>l.Id == id && l.UserId == userId, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.LogGetItem, ex.Message);
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task Add(LogEntry logEntry, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _logContextFactory.CreateDbContextAsync(cancellationToken);
            await context.LogEntries.AddAsync(logEntry, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var message = ex.InnerException is null ? "An error occurred saving changes." : ex.InnerException.Message;
            var errorMessage = new ErrorMessage((int)ErrorCode.LogAdd, message);
            Console.WriteLine(errorMessage.ToString());
        }
    }

    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            await using var context = await _logContextFactory.CreateDbContextAsync();
            return await context.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }
}