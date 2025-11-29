using FlowSynx.Domain.Log;
using FlowSynx.Infrastructure.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace FlowSynx.Infrastructure.Logging.DatabaseLogger;

public sealed class SerilogDatabaseProviderBuilder : ILogProviderBuilder
{
    private readonly IServiceProvider _serviceProvider;

    public SerilogDatabaseProviderBuilder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ILoggerProvider? Build(
        string name, 
        Application.Configuration.System.Logger.LoggerProviderConfiguration? config)
    {
        var level = config?.LogLevel.ToSerilogLevel() ?? Serilog.Events.LogEventLevel.Information;
        var loggerService = _serviceProvider.GetRequiredService<ILoggerService>();
        var accessor = _serviceProvider.GetRequiredService<IHttpContextAccessor>();

        var logger = new LoggerConfiguration()
            .MinimumLevel.Is(level)
            .WriteTo.SqliteLogs(loggerService, accessor)
            .CreateLogger();

        return new SerilogLoggerProvider(logger, dispose: true);
    }
}