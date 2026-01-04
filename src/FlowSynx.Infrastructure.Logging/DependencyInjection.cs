using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Logging;

public static class DependencyInjection
{
    public static IServiceCollection AddLoggers(this IServiceCollection services)
    {
        services.AddScoped<ITenantLoggerFactory, SerilogTenantLoggerFactory>();
        services.AddScoped<ILoggerProvider, TenantLoggerProvider>();
        return services;
    }
}