using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs.Logging;
using Serilog;

namespace FlowSynx.Infrastructure.Logging.ConsoleLogger;

public sealed class ConsoleSinkConfigurator : ILoggingSinkConfigurator
{
    public LoggerConfiguration Configure(LoggerConfiguration configuration, TenantId tenantId, TenantLoggingPolicy policy)
    {
        return configuration.WriteTo.Console(
            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{TenantId}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
    }
}