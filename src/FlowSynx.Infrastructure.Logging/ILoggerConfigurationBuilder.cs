using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs.Logging;
using Serilog;

namespace FlowSynx.Infrastructure.Logging;

public interface ILoggerConfigurationBuilder
{
    LoggerConfiguration Build(TenantId tenantId, TenantLoggingPolicy policy);
}