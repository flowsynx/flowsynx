using FlowSynx.Infrastructure.Security.Secrets.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace FlowSynx.Infrastructure.Logging;

public static class DependencyInjection
{
    public static IServiceCollection AddLoggers(this IServiceCollection services)
    {
        //services.AddScoped<ITenantLoggerFactory, SerilogTenantLoggerFactory>();
        //services.AddScoped<ILoggerProvider, TenantLoggerProvider>();
        services.AddSingleton<TenantLoggingService>();

        services.AddSingleton<ILoggerFactory, TenantAwareLoggerFactory>(sp =>
        {
            var defaultFactory = LoggerFactory.Create(loggingBuilder =>
            {
                loggingBuilder.AddSerilog(Log.Logger);
            });

            return new TenantAwareLoggerFactory(
                defaultFactory,
                sp);
        });

        return services;
    }
}