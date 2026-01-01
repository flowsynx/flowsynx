using FlowSynx.Domain.Tenants;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlowSynx.Infrastructure.Logging;

public interface ITenantLoggerFactory
{
    ILogger GetLogger(TenantId tenantId);
}
