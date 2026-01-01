using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.Tenants.ValueObjects;
using Serilog;

namespace FlowSynx.Infrastructure.Logging;

public interface ILoggingSinkConfigurator
{
    LoggerConfiguration Configure(LoggerConfiguration configuration, TenantId tenantId, LoggingConfiguration config);
}