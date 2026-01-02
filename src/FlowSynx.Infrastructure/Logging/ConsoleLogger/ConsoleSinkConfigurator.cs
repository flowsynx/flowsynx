using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.Tenants.ValueObjects;
using Serilog;

namespace FlowSynx.Infrastructure.Logging.ConsoleLogger;

public sealed class ConsoleSinkConfigurator : ILoggingSinkConfigurator
{
    public LoggerConfiguration Configure(LoggerConfiguration configuration, TenantId tenantId, LoggingConfiguration config)
    {
        return configuration.WriteTo.Console(
            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{TenantId}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
    }
}