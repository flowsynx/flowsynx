using FlowSynx.Application.Tenancy;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs.Logging;
using FlowSynx.Infrastructure.Logging.Extensions;
using FlowSynx.Infrastructure.Security.Secrets.Extensions;
using FlowSynx.Infrastructure.Security.Secrets.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Collections.Concurrent;

namespace FlowSynx.Infrastructure.Logging;

public class TenantLoggerFactory : Microsoft.Extensions.Logging.ILoggerFactory, IDisposable
{
    private readonly ConcurrentDictionary<string, Microsoft.Extensions.Logging.ILogger> _tenantLoggers = new();
    private readonly ILoggerFactory _defaultFactory;
    private readonly IServiceProvider _serviceProvider;

    // Optional suppression for internal logger creation
    private static readonly AsyncLocal<bool> _suppressTenantLogger = new();

    public TenantLoggerFactory(
        ILoggerFactory defaultFactory, 
        IServiceProvider serviceProvider)
    {
        _defaultFactory = defaultFactory ?? throw new ArgumentNullException(nameof(defaultFactory));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public void AddProvider(ILoggerProvider provider) => _defaultFactory.AddProvider(provider);

    public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) =>
        new TenantAwareLogger(categoryName, this, _serviceProvider.GetRequiredService<ITenantContext>());

    internal Microsoft.Extensions.Logging.ILogger GetOrCreateTenantLogger(string categoryName, TenantId tenantId)
    {
        var key = $"{tenantId}:{categoryName}";
        return _tenantLoggers.GetOrAdd(key, _ => CreateTenantSpecificLogger(categoryName, tenantId));
    }

    private Microsoft.Extensions.Logging.ILogger CreateTenantSpecificLogger(string categoryName, TenantId tenantId)
    {
        var previous = _suppressTenantLogger.Value;
        _suppressTenantLogger.Value = true;

        try
        {
            var spf = _serviceProvider.GetRequiredService<ISecretProviderFactory>();
            var provider = spf.GetProviderForTenantAsync(tenantId).ConfigureAwait(false).GetAwaiter().GetResult();
            var secrets = provider.GetSecretsAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            var loggingPolicy = secrets.GetLoggingPolicy();

            var loggerConfig = new Serilog.LoggerConfiguration()
                .Enrich.WithProperty("TenantId", tenantId)
                .MinimumLevel.Is(loggingPolicy?.DefaultLogLevel.GetLogEventLevel() ?? Serilog.Events.LogEventLevel.Information)
                .Enrich.FromLogContext();

            ConfigureFileSink(loggerConfig, loggingPolicy?.File, tenantId);
            ConfigureSeqSink(loggerConfig, loggingPolicy?.Seq);

            var serilogLogger = loggerConfig.CreateLogger().ForContext("SourceContext", categoryName);
            return new SerilogLoggerWrapper(serilogLogger);
        }
        finally
        {
            _suppressTenantLogger.Value = previous;
        }
    }

    private static void ConfigureFileSink(Serilog.LoggerConfiguration config, TenantFileLoggingPolicy? filePolicy, TenantId tenantId)
    {
        if (filePolicy == null || string.IsNullOrWhiteSpace(filePolicy.LogPath)) return;

        var logPath = Path.Combine(filePolicy.LogPath, "log-.txt");
        config.WriteTo.File(
            path: logPath,
            restrictedToMinimumLevel: LogLevelMapper.Map(filePolicy.LogLevel),
            outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [Tenant:{TenantId}] [Thread:{ThreadId}] " +
                "[Machine:{MachineName}] [Process:{ProcessName}:{ProcessId}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
            rollingInterval: filePolicy.RollingInterval.RollingIntervalFromString(),
            retainedFileCountLimit: filePolicy.RetainedFileCountLimit ?? 7,
            shared: true
        );
    }

    private static void ConfigureSeqSink(Serilog.LoggerConfiguration config, TenantSeqLoggingPolicy? seqPolicy)
    {
        if (seqPolicy == null || string.IsNullOrWhiteSpace(seqPolicy.Url)) return;

        config.WriteTo.Seq(
            serverUrl: seqPolicy.Url!,
            apiKey: string.IsNullOrWhiteSpace(seqPolicy.ApiKey) ? null : seqPolicy.ApiKey,
            restrictedToMinimumLevel: LogLevelMapper.Map(seqPolicy.LogLevel)
        );
    }

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

    private class TenantAwareLogger : Microsoft.Extensions.Logging.ILogger
    {
        private readonly string _categoryName;
        private readonly TenantLoggerFactory _factory;
        private readonly ITenantContext _tenantContext;

        public TenantAwareLogger(string categoryName, TenantLoggerFactory factory, ITenantContext tenantContext)
        {
            _categoryName = categoryName;
            _factory = factory;
            _tenantContext = tenantContext;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            var tenantId = _tenantContext.TenantId;
            return tenantId?.HasValue == true
                ? Serilog.Context.LogContext.PushProperty("TenantId", tenantId.ToString())
                : NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (_suppressTenantLogger.Value)
            {
                _factory._defaultFactory.CreateLogger(_categoryName).Log(logLevel, eventId, state, exception, formatter);
                return;
            }

            var tenantId = _tenantContext.TenantId;
            var logger = tenantId?.HasValue == true
                ? _factory.GetOrCreateTenantLogger(_categoryName, tenantId)
                : _factory._defaultFactory.CreateLogger(_categoryName);

            logger.Log(logLevel, eventId, state, exception, formatter);
        }

        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();
            public void Dispose() { }
        }
    }

    private class SerilogLoggerWrapper : Microsoft.Extensions.Logging.ILogger
    {
        private readonly Serilog.ILogger _serilogLogger;

        public SerilogLoggerWrapper(Serilog.ILogger serilogLogger) => _serilogLogger = serilogLogger;

        public IDisposable BeginScope<TState>(TState state) =>
            Serilog.Context.LogContext.PushProperty("Scope", state?.ToString() ?? string.Empty);

        public bool IsEnabled(LogLevel logLevel) => _serilogLogger.IsEnabled(ConvertLogLevel(logLevel));

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var serilogLevel = ConvertLogLevel(logLevel);
            if (!_serilogLogger.IsEnabled(serilogLevel)) return;

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
