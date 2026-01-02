using FlowSynx.Application.Core.Serializations;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSynx.Infrastructure.Serializations.Json;

public static class DependencyInjection
{
    public static IServiceCollection AddJsonSerialization(this IServiceCollection services)
    {
        services
            .AddSingleton<INormalizer, JsonNormalizer>()
            .AddSingleton<IObjectParser, JsonObjectParser>()
            .AddSingleton<ISerializer, JsonSerializer>()
            .AddSingleton<IDeserializer, JsonDeserializer>();

        return services;
    }
}
