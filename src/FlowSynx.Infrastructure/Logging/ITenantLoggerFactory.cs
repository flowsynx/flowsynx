using FlowSynx.Domain.Tenants;
using Serilog;

namespace FlowSynx.Infrastructure.Logging;

public interface ITenantLoggerFactory
{
    ILogger GetLogger(TenantId tenantId);
}
