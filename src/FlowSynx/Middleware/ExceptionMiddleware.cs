using System.Net;
using FlowSynx.Application.Serialization;
using FlowSynx.Application.Wrapper;
using FlowSynx.PluginCore.Exceptions;

namespace FlowSynx.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IJsonSerializer _serializer;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IJsonSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(next);
        _next = next;
        _logger = logger;
        _serializer = serializer;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception error)
        {
            var response = context.Response;
            response.ContentType = "application/json";
            var responseModel = new Result<string>() { Succeeded = false, Messages = new List<string>() { error.Message } };

            switch (error)
            {
                case FlowSynxException e:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    responseModel.Messages = new List<string> { e.ToString() };
                    break;
                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            var result = _serializer.Serialize(responseModel);

            if (!string.IsNullOrEmpty(result))
                _logger.LogError(result);

            await response.WriteAsync(result);
        }
    }
}