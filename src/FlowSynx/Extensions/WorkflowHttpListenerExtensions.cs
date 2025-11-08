using FlowSynx.Application.Services;
using FlowSynx.Infrastructure.Workflow.Triggers.HttpBased;

namespace FlowSynx.Extensions;

public static class WorkflowHttpListenerExtensions
{
    public static void MapHttpTriggersWorkflowRoutes(
        this WebApplication app,
        IWorkflowHttpListener listener,
        string routeGroup = "triggers")
    {
        // Normalize the route group
        if (!routeGroup.StartsWith("/")) routeGroup = "/" + routeGroup;
        if (routeGroup.EndsWith("/"))  routeGroup = routeGroup.TrimEnd('/');

        var group = app.MapGroup(routeGroup);

        group.Map("{**catchAll}", async context =>
        {
            var userService = context.RequestServices.GetRequiredService<ICurrentUserService>();
            userService.ValidateAuthentication();
            var userId = userService.UserId();

            var path = context.Request.Path.Value ?? "/";
            var method = context.Request.Method.ToUpperInvariant();

            // Compute the relative path (excluding routeGroup)
            var relativePath = path.StartsWith(routeGroup, StringComparison.OrdinalIgnoreCase)
                ? path[routeGroup.Length..]
                : path;

            if (string.IsNullOrWhiteSpace(relativePath))
                relativePath = "/";

            if (!listener.TryGetHandler(userId, method, relativePath, out var handler) || handler is null)
            {
                await WriteResponseAsync(context, StatusCodes.Status404NotFound,
                    $"No workflow trigger found.");
                return;
            }

            var body = await ReadBodyAsync(context.Request);

            var requestData = new HttpRequestData
            {
                Method = method,
                Path = relativePath,
                Body = body,
                Headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                Query = context.Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString())
            };

            await handler(requestData);

            await WriteResponseAsync(context, StatusCodes.Status200OK,
                $"Workflow triggered successfully.");
        });
    }

    private static async Task<string> ReadBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return body;
    }

    private static async Task WriteResponseAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync(message);
    }
}
