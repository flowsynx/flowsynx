using System.Threading;
using FlowSynx.Application.Tenancy;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs.Logging;
using FlowSynx.Infrastructure.Logging.Extensions;
using FlowSynx.Infrastructure.Security.Secrets.Extensions;
using FlowSynx.Infrastructure.Security.Secrets.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace FlowSynx.Infrastructure.Logging;

public class TenantAwareLoggerFactory : Microsoft.Extensions.Logging.ILoggerFactory
{
    private static readonly AsyncLocal<bool> _suppressTenantLogger = new();
    private readonly Dictionary<string, Microsoft.Extensions.Logging.ILogger> _tenantLoggers = new();
    private readonly object _lock = new();
    //private readonly ISecretProviderFactory _secretProviderFactory;
    private readonly ILoggerFactory _defaultFactory;
    private readonly IServiceProvider _serviceProvider;

    public TenantAwareLoggerFactory(
        //ISecretProviderFactory secretProviderFactory,
        ILoggerFactory defaultFactory,
        IServiceProvider serviceProvider)
    {
        //_secretProviderFactory = secretProviderFactory ?? throw new ArgumentNullException(nameof(secretProviderFactory));
        _defaultFactory = defaultFactory ?? throw new ArgumentNullException(nameof(defaultFactory));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public void AddProvider(ILoggerProvider provider) =>
        _defaultFactory.AddProvider(provider);

    public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
    {
        return new TenantAwareLogger(
            categoryName,
            this,
            _serviceProvider.GetRequiredService<ITenantContext>());
    }

    internal Microsoft.Extensions.Logging.ILogger GetOrCreateTenantLogger(string categoryName, TenantId tenantId)
    {
        var key = $"{tenantId}:{categoryName}";

        lock (_lock)
        {
            if (!_tenantLoggers.TryGetValue(key, out var logger))
            {
                logger = CreateTenantSpecificLogger(categoryName, tenantId);
                _tenantLoggers[key] = logger;
            }
            return logger;
        }
    }

    private Microsoft.Extensions.Logging.ILogger CreateTenantSpecificLogger(string categoryName, TenantId tenantId)
    {
        bool prev = _suppressTenantLogger.Value;
        _suppressTenantLogger.Value = true;
        try
        {
            var spf = _serviceProvider.GetRequiredService<ISecretProviderFactory>();
            var provider = spf.GetProviderForTenantAsync(tenantId).ConfigureAwait(false).GetAwaiter().GetResult();
            var secrets = provider.GetSecretsAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            TenantLoggingPolicy parsedLoggingPolicy = secrets.GetLoggingPolicy();

            var loggerConfiguration = new Serilog.LoggerConfiguration()
                .Enrich.WithProperty("TenantId", tenantId)
                .Enrich.FromLogContext();

            if (parsedLoggingPolicy != null && parsedLoggingPolicy.Enabled == true)
            {
                var fileLogPolicies = parsedLoggingPolicy.File;
                if (!string.IsNullOrWhiteSpace(fileLogPolicies.LogPath))
                {
                    var logPath = Path.Combine(fileLogPolicies.LogPath, "log-.txt");
                    loggerConfiguration.WriteTo.File(
                        path: logPath,
                        restrictedToMinimumLevel: LogLevelMapper.Map(fileLogPolicies.LogLevel),
                        outputTemplate:
                            "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [Tenant:{TenantId}] [Thread:{ThreadId}] " +
                            "[Machine:{MachineName}] [Process:{ProcessName}:{ProcessId}] [{SourceContext}] " +
                            "{Message:lj}{NewLine}{Exception}",
                        rollingInterval: fileLogPolicies.RollingInterval.RollingIntervalFromString(),
                        retainedFileCountLimit: fileLogPolicies.RetainedFileCountLimit ?? 7,
                        shared: true);
                }

                var seq = parsedLoggingPolicy.Seq;
                if (seq != null && !string.IsNullOrWhiteSpace(seq.Url))
                {
                    loggerConfiguration.WriteTo.Seq(
                        serverUrl: seq.Url!,
                        apiKey: string.IsNullOrWhiteSpace(seq.ApiKey) ? null : seq.ApiKey,
                        restrictedToMinimumLevel: LogLevelMapper.Map(seq.LogLevel));
                }
            }

            var serilogLogger = loggerConfiguration.CreateLogger();
            return new SerilogLoggerWrapper(serilogLogger.ForContext("SourceContext", categoryName));
        }
        finally
        {
            _suppressTenantLogger.Value = prev;
        }
    }

    private static Serilog.Events.LogEventLevel GetLogEventLevel(string level) => level.ToUpperInvariant() switch
    {
        "VERBOSE" => Serilog.Events.LogEventLevel.Verbose,
        "DEBUG" => Serilog.Events.LogEventLevel.Debug,
        "INFORMATION" => Serilog.Events.LogEventLevel.Information,
        "WARNING" => Serilog.Events.LogEventLevel.Warning,
        "ERROR" => Serilog.Events.LogEventLevel.Error,
        "FATAL" => Serilog.Events.LogEventLevel.Fatal,
        _ => Serilog.Events.LogEventLevel.Information
    };

    public void Dispose()
    {
        _defaultFactory.Dispose();
        foreach (var logger in _tenantLoggers.Values)
        {
            if (logger is IDisposable disposable)
                disposable.Dispose();
        }
        _tenantLoggers.Clear();
    }

    // Tenant-aware logger implementation
    private class TenantAwareLogger : Microsoft.Extensions.Logging.ILogger
    {
        private readonly string _categoryName;
        private readonly TenantAwareLoggerFactory _factory;
        private readonly ITenantContext _tenantContext;

        public TenantAwareLogger(
            string categoryName,
            TenantAwareLoggerFactory factory,
            ITenantContext tenantContext)
        {
            _categoryName = categoryName;
            _factory = factory;
            _tenantContext = tenantContext;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            var tenantId = _tenantContext.TenantId;
            if (tenantId != null && tenantId.HasValue)
            {
                return Serilog.Context.LogContext.PushProperty("TenantId", tenantId.ToString());
            }
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (_suppressTenantLogger.Value)
            {
                var systemLogger = _factory._defaultFactory.CreateLogger(_categoryName);
                systemLogger.Log(logLevel, eventId, state, exception, formatter);
                return;
            }

            var tenantId = _tenantContext.TenantId;
            if (tenantId != null && tenantId.HasValue)
            {
                var tenantLogger = _factory.GetOrCreateTenantLogger(_categoryName, tenantId);
                tenantLogger.Log(logLevel, eventId, state, exception, formatter);
            }
            else
            {
                var systemLogger = _factory._defaultFactory.CreateLogger(_categoryName);
                systemLogger.Log(logLevel, eventId, state, exception, formatter);
            }
        }

        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();
            public void Dispose() { }
        }
    }

    // Serilog logger wrapper
    private class SerilogLoggerWrapper : Microsoft.Extensions.Logging.ILogger
    {
        private readonly Serilog.ILogger _serilogLogger;

        public SerilogLoggerWrapper(Serilog.ILogger serilogLogger)
        {
            _serilogLogger = serilogLogger;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return Serilog.Context.LogContext.PushProperty(
                "Scope",
                state?.ToString() ?? string.Empty
            );
        }

        public bool IsEnabled(LogLevel logLevel) =>
            _serilogLogger.IsEnabled(ConvertLogLevel(logLevel));

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var serilogLevel = ConvertLogLevel(logLevel);

            if (!_serilogLogger.IsEnabled(serilogLevel))
                return;

            var message = formatter(state, exception);

            _serilogLogger.Write(serilogLevel, exception, message);
        }

        private static Serilog.Events.LogEventLevel ConvertLogLevel(LogLevel logLevel) => logLevel switch
        {
            LogLevel.Trace => Serilog.Events.LogEventLevel.Verbose,
            LogLevel.Debug => Serilog.Events.LogEventLevel.Debug,
            LogLevel.Information => Serilog.Events.LogEventLevel.Information,
            LogLevel.Warning => Serilog.Events.LogEventLevel.Warning,
            LogLevel.Error => Serilog.Events.LogEventLevel.Error,
            LogLevel.Critical => Serilog.Events.LogEventLevel.Fatal,
            _ => Serilog.Events.LogEventLevel.Information
        };
    }
}