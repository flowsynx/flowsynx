using FlowSynx.Domain.Log;
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;
using System.Security.Claims;

namespace FlowSynx.Infrastructure.Logging.DatabaseLogger;

internal class EfCoreLoggerSink : ILogEventSink
{
    private readonly ILoggerService _loggerService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IFormatProvider? _formatProvider;

    public EfCoreLoggerSink(
        ILoggerService loggerService,
        IHttpContextAccessor httpContextAccessor,
        IFormatProvider? formatProvider = null)
    {
        _loggerService = loggerService;
        _httpContextAccessor = httpContextAccessor;
        _formatProvider = formatProvider;
    }

    public void Emit(LogEvent logEvent)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var userId =
                httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? httpContext?.User?.Identity?.Name
                ?? "System";

            string category = logEvent.Properties.TryGetValue("SourceContext", out var value)
                ? value.ToString()?.Trim('"') ?? "Unknown"
                : "Unknown";

            string? scopeInfo = TryGetSerilogScope(logEvent);

            var entity = new LogEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Message = logEvent.RenderMessage(_formatProvider),
                Level = ToLevel(logEvent.Level),
                TimeStamp = logEvent.Timestamp.UtcDateTime,
                Exception = logEvent.Exception?.ToString(),
                Category = category,
                Scope = scopeInfo
            };

            // Fire & forget, but safe
            _ = _loggerService.Add(entity, CancellationToken.None);
        }
        catch
        {
            // NEVER throw from a sink
        }
    }

    private static string? TryGetSerilogScope(LogEvent logEvent)
    {
        if (logEvent.Properties.TryGetValue("Scope", out var scopeValue))
        {
            return scopeValue.ToString()?.Trim('"');
        }

        // Combine all properties that look like scopes
        var scopes = new List<string>();

        foreach (var kv in logEvent.Properties)
        {
            if (IsFrameworkScopeKey(kv.Key))
                continue;

            scopes.Add($"{kv.Key}={kv.Value}");
        }

        return scopes.Count == 0 ? null : string.Join(" | ", scopes);
    }

    private static bool IsFrameworkScopeKey(string key) =>
        key is "RequestId" or "ConnectionId" or "RequestPath";

    private static string ToLevel(LogEventLevel lvl) =>
        lvl switch
        {
            LogEventLevel.Verbose => "Trace",
            LogEventLevel.Debug => "Debug",
            LogEventLevel.Information => "Information",
            LogEventLevel.Warning => "Warning",
            LogEventLevel.Error => "Error",
            LogEventLevel.Fatal => "Critical",
            _ => "Information"
        };
}