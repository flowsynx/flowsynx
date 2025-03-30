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
        try
        {
            var response = await next();
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 500)
            {
                var errorMessage = new ErrorMessage((int)ErrorCode.BehaviorPerformanceLongRunning, 
                    $"FlowSynx Long Running Request: Request {typeof(TRequest).Name} took {stopwatch.ElapsedMilliseconds}ms");
                _logger.LogWarning(errorMessage.ToString());
            }
            return response;
        }
        catch (Exception)
        {
            stopwatch.Stop();
            var errorMessage = new ErrorMessage((int)ErrorCode.BehaviorPerformanceError, 
                $"FlowSynx Long Running Request: Request {typeof(TRequest).Name} failed after {stopwatch.ElapsedMilliseconds}ms");
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }
}