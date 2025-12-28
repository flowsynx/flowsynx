using FlowSynx.Application;
using FlowSynx.Infrastructure.Configuration.System.Logger;
using FlowSynx.Infrastructure.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using SerilogLoggerConfiguration = Serilog.LoggerConfiguration;

namespace FlowSynx.Infrastructure.Logging.DatabaseLogger;

public sealed class SerilogDatabaseProviderBuilder : ILogProviderBuilder
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SerilogDatabaseProviderBuilder(IServiceProvider serviceProvider)
    {
        _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
    }

    public ILoggerProvider? Build(
        string name, 
        LoggerProviderConfiguration? config)
    {
        var level = config?.LogLevel.ToSerilogLevel() ?? Serilog.Events.LogEventLevel.Information;

        // Resolve scoped services inside a scope
        using var scope = _scopeFactory.CreateScope();
        var logEntryRepository = scope.ServiceProvider.GetRequiredService<ILogEntryRepository>();
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();

        var logger = new SerilogLoggerConfiguration()
            .MinimumLevel.Is(level)
            // Better: pass factories to sink so it creates scopes per write
            .WriteTo.SqliteLogs(logEntryRepository,
                                accessor)
            .CreateLogger();

        return new SerilogLoggerProvider(logger, dispose: true);
    }
}