using System.Net;
using FlowSynx.Application.Serializations;
using FlowSynx.Domain.Primitives;
using FlowSynx.PluginCore.Exceptions;

namespace FlowSynx.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly ISerializer _serializer;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, ISerializer serializer)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
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