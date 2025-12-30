using FlowSynx.Application.Services;

namespace FlowSynx.Middleware;

public sealed class VersionHeaderMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IVersion _version;

    public VersionHeaderMiddleware(RequestDelegate next, IVersion version)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(version);
        _next = next;
        _version = version;
    }

    public Task Invoke(HttpContext context)
    {
        context.Response.Headers["flowsynx-version"] = _version.Version.ToString();
        return _next(context);
    }
}