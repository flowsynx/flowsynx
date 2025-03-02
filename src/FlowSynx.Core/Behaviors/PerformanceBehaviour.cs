using FlowSynx.Core.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FlowSynx.Core.Behaviors;

public class PerformanceBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly Stopwatch _timer;
    private readonly ILogger<TRequest> _logger;
    private readonly ICurrentUserService _currentUserService;

    public PerformanceBehaviour(ILogger<TRequest> logger, ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _timer = new Stopwatch();
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _timer.Start();

        var response = await next();

        _timer.Stop();

        var elapsedMilliseconds = _timer.ElapsedMilliseconds;

        if (elapsedMilliseconds > 500)
        {
            var requestName = typeof(TRequest).Name;
            var userId = _currentUserService.UserId ?? string.Empty;

            _logger.LogWarning("FlowSynx Long Running Request: [Name: {Name} | {ElapsedMilliseconds} milliseconds) | UserId: {@UserId}] {@Request}",
                requestName, elapsedMilliseconds, userId, request);
        }

        return response;
    }
}