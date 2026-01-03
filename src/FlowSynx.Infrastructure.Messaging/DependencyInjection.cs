using FlowSynx.Application.Core.Dispatcher;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSynx.Infrastructure.Messaging;

public static class DependencyInjection
{
    public static IServiceCollection AddDispatcher(this IServiceCollection services)
    {
        services.AddScoped<IDispatcher, Dispatcher>();
        return services;
    }
}