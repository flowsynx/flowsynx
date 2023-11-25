using FlowSync.Core.Common.Services;
using FlowSync.Core.Serialization;
using FlowSync.Infrastructure.Serialization.Json;
using FlowSync.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSync.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFlowSyncInfrastructure(this IServiceCollection services)
    {
        services.AddTransient<ISerializer, NewtonsoftSerializer>();
        services.AddTransient<IDeserializer, NewtonsoftDeserializer>();
        services.AddTransient<ISystemClock, SystemClock>();
        services.AddTransient<IOperatingSystemInfo, OperatingSystemInfo>();
        return services;
    }
}