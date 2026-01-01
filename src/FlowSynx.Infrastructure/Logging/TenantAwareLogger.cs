//using FlowSynx.Application.Services;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Logging;
//using Serilog.Context;
//using Serilog.Events;

//namespace FlowSynx.Infrastructure.Logging;

//public sealed class TenantAwareLogger : ILogger
//{
//    private readonly string _categoryName;
//    private readonly TenantAwareLoggerProvider _provider;
//    private readonly ITenantService _tenantService;

//    public TenantAwareLogger(string categoryName, TenantAwareLoggerProvider provider, ITenantService tenantService)
//    {
//        _categoryName = categoryName;
//        _provider = provider;
//        _tenantService = tenantService;
//    }

//    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
//    {
//        return null; // Use Serilog's LogContext if needed
//    }

//    public bool IsEnabled(LogLevel logLevel)
//    {
//        var tenantId = _tenantService.GetCurrentTenantId();
//        var serilogLogger = _provider.GetTenantLogger(tenantId, _categoryName);
//        var serilogLevel = ConvertLogLevel(logLevel);

//        return serilogLogger.IsEnabled(serilogLevel);
//    }

//    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
//        Exception? exception, Func<TState, Exception?, string> formatter)
//    {
//        var tenantId = _tenantService.GetCurrentTenantId();
//        var serilogLogger = _provider.GetTenantLogger(tenantId, _categoryName);
//        var serilogLevel = ConvertLogLevel(logLevel);

//        if (!serilogLogger.IsEnabled(serilogLevel)) return;

//        var message = formatter(state, exception);
//        var properties = new List<LogEventProperty>();

//        // Add structured properties from state
//        if (state is IReadOnlyList<KeyValuePair<string, object>> logValues)
//        {
//            foreach (var kvp in logValues)
//            {
//                if (kvp.Key != "{OriginalFormat}" && kvp.Value != null)
//                {
//                    properties.Add(new LogEventProperty(kvp.Key, new ScalarValue(kvp.Value)));
//                }
//            }
//        }

//        var logEvent = new LogEvent(
//            DateTimeOffset.Now,
//            serilogLevel,
//            exception,
//            MessageTemplate.Parse(message),
//            properties);

//        serilogLogger.Write(logEvent);
//    }

//    private static LogEventLevel ConvertLogLevel(LogLevel logLevel) => logLevel switch
//    {
//        LogLevel.Trace => LogEventLevel.Verbose,
//        LogLevel.Debug => LogEventLevel.Debug,
//        LogLevel.Information => LogEventLevel.Information,
//        LogLevel.Warning => LogEventLevel.Warning,
//        LogLevel.Error => LogEventLevel.Error,
//        LogLevel.Critical => LogEventLevel.Fatal,
//        _ => LogEventLevel.Information
//    };
//}

////public class TenantAwareLogger<T> : ILogger<T>
////{
////    private readonly ILogger _logger;
////    private readonly IHttpContextAccessor _httpContextAccessor;

////    public TenantAwareLogger(ILoggerFactory loggerFactory, IHttpContextAccessor httpContextAccessor)
////    {
////        _logger = loggerFactory.CreateLogger<T>();
////        _httpContextAccessor = httpContextAccessor;
////    }

////    public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);

////    public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

////    public void Log<TState>(
////        LogLevel logLevel,
////        EventId eventId,
////        TState state,
////        Exception? exception,
////        Func<TState, Exception?, string> formatter)
////    {
////        var httpContext = _httpContextAccessor.HttpContext;
////        var tenantId = httpContext?.Items.TryGetValue("TenantId", out var value) == true ? value : null;

////        using (tenantId is not null ? LogContext.PushProperty("TenantId", tenantId) : default!)
////        {
////            _logger.Log(logLevel, eventId, state, exception, formatter);
////        }
////    }
////}