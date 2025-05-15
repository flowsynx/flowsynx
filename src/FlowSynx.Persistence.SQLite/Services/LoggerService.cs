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
                .FirstOrDefaultAsync(l=>l.Id == id && l.UserId == userId, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.LogGetItem, ex.Message);
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<IReadOnlyCollection<LogEntity>> GetWorkflowExecutionLogs(string userId, Guid workflowId, 
        Guid workflowExecutionId, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _logContextFactory.CreateDbContextAsync(cancellationToken);
            var logs = await context.Logs.Where(l=> l.UserId == userId).ToListAsync(cancellationToken).ConfigureAwait(false);

            var result = new List<LogEntity>();

            foreach (var log in logs)
            {
                if (string.IsNullOrWhiteSpace(log.Scope))
                    continue;

                var scopeParts = log.Scope.Split('|');
                var scopeDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var part in scopeParts)
                {
                    var keyValue = part.Split('=', 2, StringSplitOptions.TrimEntries);
                    if (keyValue.Length == 2)
                    {
                        scopeDict[keyValue[0]] = keyValue[1];
                    }
                }

                if (scopeDict.TryGetValue("WorkflowId", out var wId) &&
                    scopeDict.TryGetValue("WorkflowExecutionId", out var weId) &&
                    Guid.TryParse(wId, out var parsedWorkflowId) &&
                    Guid.TryParse(weId, out var parsedWorkflowExecutionId) &&
                    parsedWorkflowId == workflowId &&
                    parsedWorkflowExecutionId == workflowExecutionId)
                {
                    result.Add(log);
                }
            }


            return result;
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.LogsList, ex.Message);
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<IReadOnlyCollection<LogEntity>> GetWorkflowTaskExecutionLogs(string userId, Guid workflowId,
        Guid workflowExecutionId, Guid workflowTaskExecutionId, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _logContextFactory.CreateDbContextAsync(cancellationToken);
            var logs = await context.Logs.Where(l => l.UserId == userId).ToListAsync(cancellationToken).ConfigureAwait(false);

            var result = new List<LogEntity>();

            foreach (var log in logs)
            {
                if (string.IsNullOrWhiteSpace(log.Scope))
                    continue;

                var scopeParts = log.Scope.Split('|');
                var scopeDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var part in scopeParts)
                {
                    var keyValue = part.Split('=', 2, StringSplitOptions.TrimEntries);
                    if (keyValue.Length == 2)
                    {
                        scopeDict[keyValue[0]] = keyValue[1];
                    }
                }

                if (scopeDict.TryGetValue("WorkflowId", out var wId) &&
                    scopeDict.TryGetValue("WorkflowExecutionId", out var weId) &&
                    scopeDict.TryGetValue("WorkflowExecutionTaskId", out var wetId) &&
                    Guid.TryParse(wId, out var parsedWorkflowId) &&
                    Guid.TryParse(weId, out var parsedWorkflowExecutionId) &&
                    Guid.TryParse(wetId, out var parsedWorkflowTaskExecutionId) &&
                    parsedWorkflowId == workflowId &&
                    parsedWorkflowExecutionId == workflowExecutionId &&
                    parsedWorkflowTaskExecutionId == workflowTaskExecutionId)
                {
                    result.Add(log);
                }
            }


            return result;
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.LogsList, ex.Message);
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