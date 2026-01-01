using FlowSynx.Application.Services;
using Microsoft.Extensions.Logging;


namespace FlowSynx.Infrastructure.Logging;

public sealed class TenantLoggerProvider : ILoggerProvider
{
    private readonly ITenantService _tenantContext;
    private readonly ITenantLoggerFactory _factory;

    public TenantLoggerProvider(
        ITenantService tenantContext,
        ITenantLoggerFactory factory)
    {
        _tenantContext = tenantContext;
        _factory = factory;
    }

    public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
    {
        var tenantId = _tenantContext.GetCurrentTenantId();
        var serilog = _factory.GetLogger(tenantId);
        return new Serilog.Extensions.Logging.SerilogLoggerProvider(serilog)
            .CreateLogger(categoryName);
    }

    public void Dispose() { }
}

//public sealed class TenantAwareLoggerProvider : ILoggerProvider
//{
//    private readonly ConcurrentDictionary<string, Serilog.ILogger> _tenantLoggers = new();
//    private readonly IServiceProvider _serviceProvider;
//    private bool _disposed;

//    public TenantAwareLoggerProvider(IServiceProvider serviceProvider)
//    {
//        _serviceProvider = serviceProvider;
//    }

//    public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
//    {
//        return new TenantAwareLogger(categoryName, this);
//    }

//    public async Task<Serilog.ILogger> GetTenantLogger(TenantId tenantId, string categoryName)
//    {
//        var loggerKey = $"{tenantId}::{categoryName}";

//        return _tenantLoggers.GetOrAdd(loggerKey, async key =>
//        {
//            using var scope = _serviceProvider.CreateScope();
//            var tenantLoggingProvider = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
//            var config = await tenantLoggingProvider.GetConfigurationAsync(tenantId, CancellationToken.None);

//            return CreateTenantLogger(tenantId, categoryName, config);
//        });
//    }

//    private Serilog.ILogger CreateTenantLogger(TenantId tenantId, string categoryName,
//        TenantConfiguration? config)
//    {
//        var origins = config?.GetValue<string[]>("Cors:AllowedOrigins", ["*"]);
//        var logLevel = config?.GetValue<string>("Logging:LogLevel", "Information");

//        var loggerConfiguration = new LoggerConfiguration()
//            .MinimumLevel.Is(GetLogLevel(logLevel))
//            .Enrich.WithProperty("TenantId", tenantId)
//            .Enrich.WithProperty("SourceContext", categoryName)
//            .Enrich.FromLogContext();

//        //// Add tenant-specific properties
//        //if (config?.Properties != null)
//        //{
//        //    foreach (var prop in config.Properties)
//        //    {
//        //        loggerConfiguration.Enrich.WithProperty(prop.Key, prop.Value);
//        //    }
//        //}

//        // Configure Seq sink if available
//        var seqServerUrl = config?.GetValue<string>("Logging:Seq:Url", "http://localhost:5341/");
//        var seqApiKey = config?.GetValue<string>("Logging:Seq:ApiKey", null);

//        if (!string.IsNullOrEmpty(seqServerUrl))
//        {
//            loggerConfiguration.WriteTo.Seq(
//                seqServerUrl,
//                apiKey: seqApiKey,
//                restrictedToMinimumLevel: GetLogLevel(logLevel),
//                controlLevelSwitch: null);
//        }

//        return loggerConfiguration.CreateLogger();
//    }

//    private static LogEventLevel GetLogLevel(string? level) => level?.ToLower() switch
//    {
//        "verbose" or "trace" => LogEventLevel.Verbose,
//        "debug" => LogEventLevel.Debug,
//        "information" or "info" => LogEventLevel.Information,
//        "warning" or "warn" => LogEventLevel.Warning,
//        "error" => LogEventLevel.Error,
//        "fatal" or "critical" => LogEventLevel.Fatal,
//        _ => LogEventLevel.Information
//    };

//    public void Dispose()
//    {
//        if (!_disposed)
//        {
//            foreach (var logger in _tenantLoggers.Values)
//            {
//                (logger as IDisposable)?.Dispose();
//            }
//            _tenantLoggers.Clear();
//            _disposed = true;
//        }
//    }
//}