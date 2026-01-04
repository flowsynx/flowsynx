using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs.Logging;
using Serilog;

namespace FlowSynx.Infrastructure.Logging;

public interface ILoggingSinkConfigurator
{
    LoggerConfiguration Configure(LoggerConfiguration configuration, TenantId tenantId, TenantLoggingPolicy policy);
}