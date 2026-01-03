//using MediatR;
//using Microsoft.Extensions.Logging;
//using System.Diagnostics;

//namespace FlowSynx.Application.Core.Behaviors;

//public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
//{
//    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

//    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
//    {
//        ArgumentNullException.ThrowIfNull(logger);
//        _logger = logger;
//    }

//    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
//    {
//        var stopwatch = Stopwatch.StartNew();
//        var response = await next(cancellationToken);
//        stopwatch.Stop();

//        if (stopwatch.ElapsedMilliseconds <= 500) 
//            return response;

//        _logger.LogInformation("FlowSynx Long Running Request: Request {Request} took {Milliseconds}ms", 
//            typeof(TRequest).Name, stopwatch.ElapsedMilliseconds);

//        return response;
//    }
//}