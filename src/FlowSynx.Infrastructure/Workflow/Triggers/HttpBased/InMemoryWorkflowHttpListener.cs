using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace FlowSynx.Infrastructure.Workflow.Triggers.HttpBased;

public class InMemoryWorkflowHttpListener : IWorkflowHttpListener
{
    private readonly ILogger<InMemoryWorkflowHttpListener> _logger;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Func<HttpRequestData, Task>>> _handlers;

    public InMemoryWorkflowHttpListener(
        ILogger<InMemoryWorkflowHttpListener> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _handlers = new();
    }

    public void RegisterRoute(string userId, string method, string route, Func<HttpRequestData, Task> handler)
    {
        method = method.ToUpperInvariant();
        route = route.StartsWith("/") ? route : "/" + route;

        var key = GetHandlerKey(method, route);
        var userMap = _handlers.GetOrAdd(userId, _ => new());
        if (!userMap.TryAdd(key, handler))
            return;
        
        _logger.LogInformation("Registered workflow HTTP trigger for user {UserId}: {Method} {Route}", userId, method, route);
    }

    public bool TryGetHandler(string userId, string method, string path, out Func<HttpRequestData, Task>? handler)
    {
        // Normalize inputs the same way as in RegisterRoute to ensure key match
        handler = null;

        if (!_handlers.TryGetValue(userId, out var userMap))
            return false;

        var key = GetHandlerKey(method, path);
        return userMap.TryGetValue(key, out handler);
    }

    private static string GetHandlerKey(string method, string route)
        => $"{method}:{route.TrimEnd('/')}";
}