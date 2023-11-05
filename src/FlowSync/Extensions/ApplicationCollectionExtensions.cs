using Asp.Versioning;
using Asp.Versioning.Builder;
using FlowSync.Middleware;

namespace FlowSync.Extensions;

public static class ApplicationCollectionExtensions
{
    public static IApplicationBuilder ConfigureCustomException(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionMiddleware>();
        return app;
    }
}