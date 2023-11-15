using FlowSync.Models;

namespace FlowSync.Middleware;

public class CustomHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly CustomHeadersToAddAndRemove _headers;


    public CustomHeadersMiddleware(RequestDelegate next, CustomHeadersToAddAndRemove headers)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _headers = headers;
    }

    public async Task Invoke(HttpContext context)
    {
        foreach (var headerValuePair in _headers.HeadersToAdd)
        {
            context.Response.Headers[headerValuePair.Key] = headerValuePair.Value;
        }
        foreach (var header in _headers.HeadersToRemove)
        {
            context.Response.Headers.Remove(header);
        }
        
        await _next(context);
    }
}