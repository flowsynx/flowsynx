using System.Net;
using EnsureThat;
using FlowSync.Core.Common.Models;
using FlowSync.Core.Exceptions;
using FlowSync.Core.Serialization;

namespace FlowSync.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly ISerializer _serializer;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, ISerializer serializer)
    {
        this._next = next;
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(serializer, nameof(serializer));
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
            response.ContentType = _serializer.ContentMineType;
            var responseModel = new Result<string>() { Succeeded = false, Messages = new List<string>() { error.Message } };

            switch (error)
            {
                case ApiBaseException e:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
                case InputValidationException e:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    responseModel.Messages = e.Errors;
                    break;
                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            var result = _serializer.Serialize(responseModel);
            await response.WriteAsync(result);
        }
    }
}