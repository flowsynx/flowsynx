using FlowSynx.Application.Tenancy;
using Microsoft.Extensions.Logging;


namespace FlowSynx.Infrastructure.Logging;

public sealed class TenantLoggerProvider : ILoggerProvider
{
    private readonly ITenantContext _tenantContext;
    private readonly ITenantLoggerFactory _factory;

    public TenantLoggerProvider(
        ITenantContext tenantContext,
        ITenantLoggerFactory factory)
    {
        _tenantContext = tenantContext;
        _factory = factory;
    }

    public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
    {
        var tenantId = _tenantContext.TenantId;
        var serilog = _factory.GetLogger(tenantId);
        return new Serilog.Extensions.Logging.SerilogLoggerProvider(serilog)
            .CreateLogger(categoryName);
    }

    public void Dispose() { }
}