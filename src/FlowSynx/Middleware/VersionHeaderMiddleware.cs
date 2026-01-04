using FlowSynx.Application.Abstractions.Services;

namespace FlowSynx.Middleware;

public sealed class VersionHeaderMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IVersion _version;

    public VersionHeaderMiddleware(RequestDelegate next, IVersion version)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _version = version ?? throw new ArgumentNullException(nameof(version));
    }

    public Task Invoke(HttpContext context)
    {
        context.Response.Headers["flowsynx-version"] = _version.Version.ToString();
        return _next(context);
    }
}