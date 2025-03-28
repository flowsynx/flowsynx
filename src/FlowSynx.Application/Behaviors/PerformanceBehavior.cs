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
                _logger.LogWarning("FlowSynx Long Running Request: Request {RequestName} took {ElapsedMilliseconds}ms", typeof(TRequest).Name, stopwatch.ElapsedMilliseconds);
            }
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "FlowSynx Long Running Request: Request {RequestName} failed after {ElapsedMilliseconds}ms", typeof(TRequest).Name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}