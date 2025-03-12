using FlowSynx.Application.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Behaviors;

public class UnhandledExceptionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly ILogger<TRequest> _logger;

    public UnhandledExceptionBehaviour(ILogger<TRequest> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (InputValidationException ive)
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogWarning("Validation error occurred in request '{RequestName}'\r\n\tRequestPayload: {@RequestPayload}\r\n\tErrors: {@Errors}.", requestName, request, ive.Errors);
            throw;
        }
        catch (Exception e)
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogError(e, "Exception occurred in request '{RequestName}'\r\n\tRequestPayload: {@RequestPayload}", requestName, request);
            throw;
        }
    }
}