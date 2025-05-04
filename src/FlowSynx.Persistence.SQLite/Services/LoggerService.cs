using Microsoft.EntityFrameworkCore;
using FlowSynx.Persistence.SQLite.Contexts;
using System.Linq.Expressions;
using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Domain.Log;

namespace FlowSynx.Persistence.SQLite.Services;

public class LoggerService : ILoggerService
{
    private readonly IDbContextFactory<LoggerContext> _logContextFactory;

    public LoggerService(IDbContextFactory<LoggerContext> logContextFactory)
    {
        ArgumentNullException.ThrowIfNull(logContextFactory);
        _logContextFactory = logContextFactory;
    }

    public async Task<IReadOnlyCollection<LogEntity>> All(Expression<Func<LogEntity, bool>>? predicate, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _logContextFactory.CreateDbContextAsync(cancellationToken);
            var logs = context.Logs;

            if (predicate == null)
                return await logs.OrderBy(x=>x.TimeStamp).Take(250).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            else
                return await logs.Where(predicate).OrderBy(x => x.TimeStamp).Take(250).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.LogsList, ex.Message);
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<LogEntity?> Get(string userId, Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _logContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Logs
                .FindAsync(new object?[] { userId, id }, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.LogGetItem, ex.Message);
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task Add(LogEntity logEntity, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _logContextFactory.CreateDbContextAsync(cancellationToken);
            await context.Logs.AddAsync(logEntity, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var message = ex.InnerException is null ? Resources.LoggerService_AddLogError: ex.InnerException.Message;
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