using FlowSynx.Application;
using FlowSynx.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;
using System.Globalization;
using System.Security.Claims;

namespace FlowSynx.Infrastructure.Logging.DatabaseLogger;

internal class SqliteLoggerSink : ILogEventSink
{
    private readonly ILogEntryRepository _logEntryRepository;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IFormatProvider? _formatProvider;

    public SqliteLoggerSink(
        ILogEntryRepository logEntryRepository,
        IHttpContextAccessor? httpContextAccessor,
        IFormatProvider? formatProvider = null)
    {
        _logEntryRepository = logEntryRepository ?? throw new ArgumentNullException(nameof(logEntryRepository));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _formatProvider = formatProvider ??CultureInfo.InvariantCulture;
    }

    public void Emit(LogEvent logEvent)
    {
        try
        {
            var httpContext = _httpContextAccessor?.HttpContext;

            var userId =
                httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? httpContext?.User?.Identity?.Name
                ?? "System";

            string category = logEvent.Properties.TryGetValue("SourceContext", out var value)
                ? value.ToString()?.Trim('"') ?? "Unknown"
                : "Unknown";

            string? scopeInfo = TryGetSerilogScope(logEvent);

            var entity = new LogEntry
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
            _ = _logEntryRepository.Add(entity, CancellationToken.None);
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