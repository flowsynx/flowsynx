using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace FlowSynx.Infrastructure.Logging;

public static class DependencyInjection
{
    public static IServiceCollection AddLoggers(this IServiceCollection services)
    {
        services.AddSingleton<TenantLoggingService>();

        services.AddSingleton<ILoggerFactory, TenantLoggerFactory>(sp =>
        {
            var defaultFactory = LoggerFactory.Create(loggingBuilder =>
            {
                loggingBuilder.AddSerilog(Log.Logger);
            });

            return new TenantLoggerFactory(
                defaultFactory,
                sp);
        });

        return services;
    }
}