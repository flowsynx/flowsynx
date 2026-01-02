using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.Tenants.ValueObjects;
using Serilog;

namespace FlowSynx.Infrastructure.Logging;

public interface ILoggerConfigurationBuilder
{
    LoggerConfiguration Build(TenantId tenantId, LoggingConfiguration config);
}