using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FlowSynx.Application.Behaviors;

public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await next(cancellationToken);
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds <= 500) 
            return response;

        var message = string.Format(Resources.PerformanceBehavior_LongRunning_Request, 
            typeof(TRequest).Name, stopwatch.ElapsedMilliseconds);
        var errorMessage = new ErrorMessage((int)ErrorCode.BehaviorPerformanceLongRunning, message);;
        _logger.LogDebug(errorMessage.ToString());

        return response;
    }
}