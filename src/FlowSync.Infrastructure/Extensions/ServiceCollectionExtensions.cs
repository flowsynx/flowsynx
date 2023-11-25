﻿using FlowSync.Core.Serialization;
using FlowSync.Core.Services;
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

        return services;
    }
}