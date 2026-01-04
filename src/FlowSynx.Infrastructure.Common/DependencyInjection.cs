using FlowSynx.Application.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSynx.Infrastructure.Common;

public static class DependencyInjection
{
    public static IServiceCollection AddSystemClock(this IServiceCollection services)
    {
        services.AddSingleton<ISystemClock, SystemClock>();
        return services;
    }
}
